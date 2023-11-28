import { ClipboardCopyElement } from './clipboard-copy-element.js';
const root = (typeof globalThis !== 'undefined' ? globalThis : window);
try {
    root.ClipboardCopyElement = ClipboardCopyElement.define();
}
catch (e) {
    if (!(root.DOMException && e instanceof DOMException && e.name === 'NotSupportedError') &&
        !(e instanceof ReferenceError)) {
        throw e;
    }
}
export default ClipboardCopyElement;
export * from './clipboard-copy-element.js';
