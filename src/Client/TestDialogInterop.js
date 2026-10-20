const previousOverflow = new WeakMap();

/// Restores the background scrolling state after the native dialog closes.
function restoreScroll(dialog) {
    if (previousOverflow.has(dialog)) {
        document.body.style.overflow = previousOverflow.get(dialog);
        previousOverflow.delete(dialog);
    }
}

/// Uses the native modal dialog for focus trapping, background inertness and focus restoration.
export function setDialogOpen(dialog, visible) {
    if (visible && !dialog.open) {
        previousOverflow.set(dialog, document.body.style.overflow);
        dialog.showModal();
        document.body.style.overflow = "hidden";
    } else if (!visible && dialog.open) {
        dialog.close();
        restoreScroll(dialog);
    }
}

/// Binds Escape, backdrop clicks and native closing to the application state.
export function bindDialog(dialog, onClose) {
    const cancel = event => { event.preventDefault(); onClose(); };
    const close = () => { restoreScroll(dialog); onClose(); };
    const backdrop = event => {
        const bounds = dialog.getBoundingClientRect();
        if (event.target === dialog && (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom)) onClose();
    };
    dialog.addEventListener("cancel", cancel);
    dialog.addEventListener("close", close);
    dialog.addEventListener("click", backdrop);
    return () => {
        dialog.removeEventListener("cancel", cancel);
        dialog.removeEventListener("close", close);
        dialog.removeEventListener("click", backdrop);
        setDialogOpen(dialog, false);
    };
}

/// Moves within the dialog to a chapter and gives its heading keyboard focus.
export function scrollChapter(dialog, chapter) {
    const section = dialog.querySelector(`[data-chapter="${chapter}"]`);
    if (section) {
        section.scrollIntoView({ block: "start" });
        section.querySelector("h2")?.focus({ preventScroll: true });
    }
}
