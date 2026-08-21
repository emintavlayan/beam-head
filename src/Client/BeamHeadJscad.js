import modeling from "@jscad/modeling";
import renderer from "@jscad/regl-renderer";
import stlSerializer from "@jscad/stl-serializer";

const { primitives } = modeling;
const { cameras, controls, drawCommands, entitiesFromSolids, prepareRender } = renderer;
const { serialize } = stlSerializer;

export function createCuboid(width, depth, height) {
    return primitives.cuboid({ size: [width, depth, height] });
}

export function downloadStl(fileName, geometry) {
    const data = serialize({ binary: true }, geometry);
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
        position: [180, 180, 140],
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
    const renderOptions = {
        camera,
        drawCommands: {
            drawMesh: drawCommands.drawMesh,
        },
        entities: entitiesFromSolids(
            { color: [0.05, 0.55, 0.58, 1], smoothNormals: false },
            geometry,
        ),
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
        rotateDelta[1] += event.clientY - lastY;
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
