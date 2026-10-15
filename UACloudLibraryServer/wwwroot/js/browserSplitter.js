// Lets the user drag the divider between the nodeset tree pane and the details pane.
// Uses event delegation on document so it keeps working after Blazor re-renders the page.
(function () {
    const STORAGE_KEY = "uacl-browser-tree-width";
    const MIN_WIDTH = 240;

    function layoutOf(splitter) {
        return splitter.closest(".uacl-browser-layout");
    }

    function applyWidth(layout, width) {
        const max = Math.max(MIN_WIDTH, window.innerWidth * 0.7);
        width = Math.min(max, Math.max(MIN_WIDTH, width));
        layout.style.setProperty("--uacl-sidebar-width", width + "px");
        return width;
    }

    function restore() {
        const layout = document.querySelector(".uacl-browser-layout");
        if (!layout) {
            return;
        }
        const saved = parseInt(window.localStorage.getItem(STORAGE_KEY), 10);
        if (!isNaN(saved)) {
            applyWidth(layout, saved);
        }
    }

    document.addEventListener("pointerdown", function (e) {
        const splitter = e.target.closest(".uacl-browser-layout__splitter");
        if (!splitter || e.button !== 0) {
            return;
        }
        const layout = layoutOf(splitter);
        const tree = layout && layout.querySelector(".uacl-browser-layout__tree");
        if (!tree) {
            return;
        }

        e.preventDefault();
        splitter.setPointerCapture(e.pointerId);
        layout.classList.add("uacl-browser-layout--resizing");
        const left = tree.getBoundingClientRect().left;
        let width = tree.getBoundingClientRect().width;

        function onMove(ev) {
            width = applyWidth(layout, ev.clientX - left);
        }

        function onUp(ev) {
            splitter.removeEventListener("pointermove", onMove);
            splitter.removeEventListener("pointerup", onUp);
            splitter.removeEventListener("pointercancel", onUp);
            splitter.releasePointerCapture(ev.pointerId);
            layout.classList.remove("uacl-browser-layout--resizing");
            window.localStorage.setItem(STORAGE_KEY, Math.round(width));
        }

        splitter.addEventListener("pointermove", onMove);
        splitter.addEventListener("pointerup", onUp);
        splitter.addEventListener("pointercancel", onUp);
    });

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", restore);
    } else {
        restore();
    }

    // Blazor replaces the prerendered markup once the circuit connects; re-apply the saved width.
    new MutationObserver(function (mutations, observer) {
        if (document.querySelector(".uacl-browser-layout") && !document.querySelector(".uacl-browser-layout").style.getPropertyValue("--uacl-sidebar-width")) {
            restore();
        }
    }).observe(document.body, { childList: true, subtree: true });
})();
