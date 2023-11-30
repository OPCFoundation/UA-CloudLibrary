import { ClipboardCopyElement } from './clipboard-copy-element.js';
type JSXBase = JSX.IntrinsicElements extends {
    span: unknown;
} ? JSX.IntrinsicElements : Record<string, Record<string, unknown>>;
declare global {
    interface Window {
        ClipboardCopyElement: typeof ClipboardCopyElement;
    }
    interface HTMLElementTagNameMap {
        'clipboard-copy': ClipboardCopyElement;
    }
    namespace JSX {
        interface IntrinsicElements {
            ['clipboard-copy']: JSXBase['span'] & Partial<Omit<ClipboardCopyElement, keyof HTMLElement>>;
        }
    }
}
export default ClipboardCopyElement;
export * from './clipboard-copy-element.js';
