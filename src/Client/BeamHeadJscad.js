import modeling from "@jscad/modeling";
import renderer from "@jscad/regl-renderer";
import stlSerializer from "@jscad/stl-serializer";

const { primitives } = modeling;
const { colorize } = modeling.colors;
const { extrudeLinear } = modeling.extrusions;
const { geom3 } = modeling.geometries;
const { mirrorX, mirrorZ, rotateX, rotateY, translate } = modeling.transforms;
const { cameras, controls, drawCommands, entitiesFromSolids, prepareRender } = renderer;
const { serialize } = stlSerializer;

const sceneGuides = {
    gridSize: [1000, 1000],
    gridTicks: [50, 5],
    axisSize: 300,
};

const initialCameraDistanceScale = 2.25;
const jawWireframeColor = [0.08, 0.12, 0.18, 1];
// Used only for duplicate edge keys; the retained vertex coordinates are unchanged.
const edgeKeyPrecision = 1e6;

export function createJaw(
    axis,
    side,
    closingAxisExtent,
    crossAxisExtent,
    thickness,
    referenceX,
    referenceY,
    referenceZ,
    apertureFaceAngleRadians,
) {
    const sideSign = side === "positive" ? 1 : -1;
    const size = axis === "x"
        ? [closingAxisExtent, crossAxisExtent, thickness]
        : [crossAxisExtent, closingAxisExtent, thickness];
    const localCentre = axis === "x"
        ? [sideSign * closingAxisExtent / 2, 0, 0]
        : [0, sideSign * closingAxisExtent / 2, 0];

    let jaw = primitives.cuboid({ size });
    jaw = translate(localCentre, jaw);
    jaw = axis === "x"
        ? rotateY(apertureFaceAngleRadians, jaw)
        : rotateX(-apertureFaceAngleRadians, jaw);
    jaw = translate([referenceX, referenceY, referenceZ], jaw);

    const viewerColor = axis === "x"
        ? [0.05, 0.55, 0.58, 1]
        : [0.88, 0.42, 0.12, 1];

    return colorize(viewerColor, jaw);
}

export function createMlcBank(
    side,
    bankSpan,
    referenceX,
    referenceY,
    referenceZ,
    profilePoints,
) {
    const bankProfile = primitives.polygon({
        points: profilePoints,
        orientation: "clockwise",
    });

    let bank = extrudeLinear({ height: bankSpan }, bankProfile);
    bank = translate([0, 0, -bankSpan / 2], bank);
    bank = rotateX(Math.PI / 2, bank);

    if (side === "negative") {
        bank = mirrorX(bank);
    }

    bank = translate([referenceX, referenceY, referenceZ], bank);

    return colorize([0.45, 0.25, 0.72, 1], bank);
}

export function downloadStl(fileName, geometries) {
    const data = serialize({ binary: true }, ...geometries);
    const blob = new Blob(data, { type: "model/stl" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");

    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.setTimeout(() => URL.revokeObjectURL(url), 0);
}

export function createViewerDisplay(geometries) {
    return mirrorZ(...geometries);
}

const coordinateKey = vertex => vertex
    .map(coordinate => Math.round(coordinate * edgeKeyPrecision))
    .join(",");

const undirectedEdgeKey = (start, end) => {
    const startKey = coordinateKey(start);
    const endKey = coordinateKey(end);

    return startKey < endKey
        ? `${startKey}|${endKey}`
        : `${endKey}|${startKey}`;
};

const jawWireframeEntity = jaw => {
    const uniqueEdges = new Map();

    // toPolygons resolves the jaw's final translation, rotation and display mirror.
    geom3.toPolygons(jaw).forEach((polygon) => {
        const vertices = polygon.vertices;

        for (let index = 0; index < vertices.length; index += 1) {
            const start = vertices[index];
            const end = vertices[(index + 1) % vertices.length];
            const key = undirectedEdgeKey(start, end);

            if (!uniqueEdges.has(key)) {
                uniqueEdges.set(key, [start, end]);
            }
        }
    });

    const positions = Array.from(uniqueEdges.values()).flat();
    // Required by drawLines; these renderer normals have no physical meaning.
    const normals = positions.map(() => [0, 0, 1]);

    return {
        geometry: {
            positions,
            normals,
            indices: positions.map((_, index) => index),
            color: jawWireframeColor,
        },
        visuals: {
            drawCmd: "drawLines",
            show: true,
            transparent: false,
        },
    };
};

export function startViewer(
    container,
    jawGeometry,
    mlcGeometry,
    viewerMode,
    sourceX,
    sourceY,
    sourceZ,
) {
    const perspectiveCamera = cameras.perspective;
    const orbitControls = controls.orbit;
    const isBeamEyeView = viewerMode === "beamEyeView";
    const camera = isBeamEyeView
        ? {
            ...perspectiveCamera.defaults,
            position: [sourceX, sourceY, sourceZ],
            target: [0, 0, 0],
            up: [0, 1, 0],
            fov: Math.PI / 6,
        }
        : {
            ...perspectiveCamera.defaults,
            position: perspectiveCamera.defaults.position.map(
                coordinate => coordinate * initialCameraDistanceScale,
            ),
            target: [0, 0, 0],
        };
    let controlState = { ...orbitControls.defaults };
    let updateView = true;
    let rotateDelta = [0, 0];
    let zoomDelta = 0;
    let pointerDown = false;
    let lastX = 0;
    let lastY = 0;
    let animationFrame;

    const updateProjection = () => {
        const width = Math.max(container.clientWidth, 1);
        const height = Math.max(container.clientHeight, 1);
        perspectiveCamera.setProjection(camera, camera, { width, height });
        perspectiveCamera.update(camera, camera);
        updateView = true;
    };

    updateProjection();

    const render = prepareRender({
        glOptions: { container },
    });
    const solidOptions = { smoothNormals: false };
    const geometryEntities = isBeamEyeView
        ? [
            ...jawGeometry.map(jawWireframeEntity),
            ...entitiesFromSolids(solidOptions, mlcGeometry),
        ]
        : entitiesFromSolids(solidOptions, jawGeometry, mlcGeometry);
    const renderOptions = {
        camera,
        drawCommands: {
            drawAxis: drawCommands.drawAxis,
            drawGrid: drawCommands.drawGrid,
            drawLines: drawCommands.drawLines,
            drawMesh: drawCommands.drawMesh,
        },
        entities: [
            {
                visuals: {
                    drawCmd: "drawGrid",
                    show: true,
                    color: [0, 0, 0, 1],
                    subColor: [0, 0, 1, 0.5],
                    fadeOut: false,
                    transparent: true,
                },
                size: sceneGuides.gridSize,
                ticks: sceneGuides.gridTicks,
            },
            {
                visuals: { drawCmd: "drawAxis", show: true },
                size: sceneGuides.axisSize,
            },
            ...geometryEntities,
        ],
        rendering: {
            background: [0.96, 0.98, 0.98, 1],
        },
    };

    const updateAndRender = () => {
        if (!isBeamEyeView && (rotateDelta[0] || rotateDelta[1])) {
            const updated = orbitControls.rotate(
                { controls: controlState, camera, speed: 0.006 },
                rotateDelta,
            );
            controlState = { ...controlState, ...updated.controls };
            rotateDelta = [0, 0];
            updateView = true;
        }

        if (!isBeamEyeView && zoomDelta) {
            const updated = orbitControls.zoom(
                { controls: controlState, camera, speed: 0.08 },
                zoomDelta,
            );
            controlState = { ...controlState, ...updated.controls };
            zoomDelta = 0;
            updateView = true;
        }

        if (updateView) {
            if (!isBeamEyeView) {
                const updated = orbitControls.update({ controls: controlState, camera });
                controlState = { ...controlState, ...updated.controls };
                camera.position = updated.camera.position;
            }

            perspectiveCamera.update(camera, camera);
            updateView = !isBeamEyeView && controlState.changed;
            render(renderOptions);
        }

        animationFrame = window.requestAnimationFrame(updateAndRender);
    };

    const pointerDownHandler = (event) => {
        pointerDown = true;
        lastX = event.clientX;
        lastY = event.clientY;
        container.setPointerCapture(event.pointerId);
    };

    const pointerMoveHandler = (event) => {
        if (!pointerDown) return;

        rotateDelta[0] += event.clientX - lastX;
        rotateDelta[1] += lastY - event.clientY;
        lastX = event.clientX;
        lastY = event.clientY;
        event.preventDefault();
    };

    const pointerUpHandler = (event) => {
        pointerDown = false;
        if (container.hasPointerCapture(event.pointerId)) {
            container.releasePointerCapture(event.pointerId);
        }
    };

    const wheelHandler = (event) => {
        zoomDelta += event.deltaY;
        event.preventDefault();
    };

    if (!isBeamEyeView) {
        container.addEventListener("pointerdown", pointerDownHandler);
        container.addEventListener("pointermove", pointerMoveHandler);
        container.addEventListener("pointerup", pointerUpHandler);
        container.addEventListener("pointercancel", pointerUpHandler);
        container.addEventListener("wheel", wheelHandler, { passive: false });
    }

    const resizeObserver = new ResizeObserver(updateProjection);
    resizeObserver.observe(container);
    animationFrame = window.requestAnimationFrame(updateAndRender);

    return () => {
        window.cancelAnimationFrame(animationFrame);
        resizeObserver.disconnect();
        if (!isBeamEyeView) {
            container.removeEventListener("pointerdown", pointerDownHandler);
            container.removeEventListener("pointermove", pointerMoveHandler);
            container.removeEventListener("pointerup", pointerUpHandler);
            container.removeEventListener("pointercancel", pointerUpHandler);
            container.removeEventListener("wheel", wheelHandler);
        }
        container.replaceChildren();
    };
}
