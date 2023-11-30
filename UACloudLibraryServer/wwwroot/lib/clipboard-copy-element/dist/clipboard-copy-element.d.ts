export declare class ClipboardCopyElement extends HTMLElement {
    static define(tag?: string, registry?: CustomElementRegistry): typeof ClipboardCopyElement;
    constructor();
    connectedCallback(): void;
    get value(): string;
    set value(text: string);
}
