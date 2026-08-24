import modeling from "@jscad/modeling";
import renderer from "@jscad/regl-renderer";
import stlSerializer from "@jscad/stl-serializer";

const { primitives } = modeling;
const { colorize } = modeling.colors;
const { rotateX, rotateY, translate } = modeling.transforms;
const { cameras, controls, drawCommands, entitiesFromSolids, prepareRender } = renderer;
const { serialize } = stlSerializer;

const sceneGuides = {
    isocentreGridSize: [500, 500],
    isocentreGridTick: 50,
    isocentreZ: 0,
    axisSize: 500,
};

const createIsocentreGridDrawCommand = (regl, params) => {
    const [width, depth] = params.size;
    const halfWidth = width / 2;
    const halfDepth = depth / 2;
    const positions = [];

    for (let x = -halfWidth; x <= halfWidth; x += params.tick) {
        positions.push(x, -halfDepth, params.z, x, halfDepth, params.z);
    }

    for (let y = -halfDepth; y <= halfDepth; y += params.tick) {
        positions.push(-halfWidth, y, params.z, halfWidth, y, params.z);
    }

    return regl({
        vert: `
            precision mediump float;
            attribute vec3 position;
            uniform mat4 view, projection;
            void main() {
                gl_Position = projection * view * vec4(position, 1.0);
            }
        `,
        frag: `
            precision mediump float;
            uniform vec4 color;
            void main() {
                gl_FragColor = color;
            }
        `,
        attributes: {
            position: positions,
        },
        uniforms: {
            color: (context, props) => props.color,
        },
        count: positions.length / 3,
        primitive: "lines",
        lineWidth: 1,
        depth: {
            enable: true,
        },
        blend: {
            enable: true,
            func: {
                src: "src alpha",
                dst: "one minus src alpha",
            },
        },
    });
};

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

export function startViewer(container, geometry) {
    const perspectiveCamera = cameras.perspective;
    const orbitControls = controls.orbit;
    const camera = {
        ...perspectiveCamera.defaults,
        position: [800, 1200, 1200],
        target: [0, 0, 0],
        up: [0, 1, 0],
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
    const renderOptions = {
        camera,
        drawCommands: {
            drawAxis: drawCommands.drawAxis,
            drawIsocentreGrid: createIsocentreGridDrawCommand,
            drawMesh: drawCommands.drawMesh,
        },
        entities: [
            {
                visuals: {
                    drawCmd: "drawIsocentreGrid",
                    show: true,
                    transparent: true,
                    color: [0.32, 0.42, 0.52, 0.45],
                },
                size: sceneGuides.isocentreGridSize,
                tick: sceneGuides.isocentreGridTick,
                z: sceneGuides.isocentreZ,
            },
            {
                visuals: { drawCmd: "drawAxis", show: true },
                size: sceneGuides.axisSize,
            },
            ...entitiesFromSolids(
                { smoothNormals: false },
                geometry,
            ),
        ],
        rendering: {
            background: [0.96, 0.98, 0.98, 1],
        },
    };

    const updateAndRender = () => {
        if (rotateDelta[0] || rotateDelta[1]) {
            const updated = orbitControls.rotate(
                { controls: controlState, camera, speed: 0.006 },
                rotateDelta,
            );
            controlState = { ...controlState, ...updated.controls };
            rotateDelta = [0, 0];
            updateView = true;
        }

        if (zoomDelta) {
            const updated = orbitControls.zoom(
                { controls: controlState, camera, speed: 0.08 },
                zoomDelta,
            );
            controlState = { ...controlState, ...updated.controls };
            zoomDelta = 0;
            updateView = true;
        }

        if (updateView) {
            const updated = orbitControls.update({ controls: controlState, camera });
            controlState = { ...controlState, ...updated.controls };
            camera.position = updated.camera.position;
            perspectiveCamera.update(camera, camera);
            updateView = controlState.changed;
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

    container.addEventListener("pointerdown", pointerDownHandler);
    container.addEventListener("pointermove", pointerMoveHandler);
    container.addEventListener("pointerup", pointerUpHandler);
    container.addEventListener("pointercancel", pointerUpHandler);
    container.addEventListener("wheel", wheelHandler, { passive: false });

    const resizeObserver = new ResizeObserver(updateProjection);
    resizeObserver.observe(container);
    animationFrame = window.requestAnimationFrame(updateAndRender);

    return () => {
        window.cancelAnimationFrame(animationFrame);
        resizeObserver.disconnect();
        container.removeEventListener("pointerdown", pointerDownHandler);
        container.removeEventListener("pointermove", pointerMoveHandler);
        container.removeEventListener("pointerup", pointerUpHandler);
        container.removeEventListener("pointercancel", pointerUpHandler);
        container.removeEventListener("wheel", wheelHandler);
        container.replaceChildren();
    };
}
