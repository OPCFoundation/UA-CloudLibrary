import { copyNode, copyText } from './clipboard.js';
async function copy(button) {
    const id = button.getAttribute('for');
    const text = button.getAttribute('value');
    function trigger() {
        button.dispatchEvent(new CustomEvent('clipboard-copy', { bubbles: true }));
    }
    if (button.getAttribute('aria-disabled') === 'true') {
        return;
    }
    if (text) {
        await copyText(text);
        trigger();
    }
    else if (id) {
        const root = 'getRootNode' in Element.prototype ? button.getRootNode() : button.ownerDocument;
        if (!(root instanceof Document || ('ShadowRoot' in window && root instanceof ShadowRoot)))
            return;
        const node = root.getElementById(id);
        if (node) {
            await copyTarget(node);
            trigger();
        }
    }
}
function copyTarget(content) {
    if (content instanceof HTMLInputElement || content instanceof HTMLTextAreaElement) {
        return copyText(content.value);
    }
    else if (content instanceof HTMLAnchorElement && content.hasAttribute('href')) {
        return copyText(content.href);
    }
    else {
        return copyNode(content);
    }
}
function clicked(event) {
    const button = event.currentTarget;
    if (button instanceof HTMLElement) {
        copy(button);
    }
}
function keydown(event) {
    if (event.key === ' ' || event.key === 'Enter') {
        const button = event.currentTarget;
        if (button instanceof HTMLElement) {
            event.preventDefault();
            copy(button);
        }
    }
}
function focused(event) {
    ;
    event.currentTarget.addEventListener('keydown', keydown);
}
function blurred(event) {
    ;
    event.currentTarget.removeEventListener('keydown', keydown);
}
export class ClipboardCopyElement extends HTMLElement {
    static define(tag = 'clipboard-copy', registry = customElements) {
        registry.define(tag, this);
        return this;
    }
    constructor() {
        super();
        this.addEventListener('click', clicked);
        this.addEventListener('focus', focused);
        this.addEventListener('blur', blurred);
    }
    connectedCallback() {
        if (!this.hasAttribute('tabindex')) {
            this.setAttribute('tabindex', '0');
        }
        if (!this.hasAttribute('role')) {
            this.setAttribute('role', 'button');
        }
    }
    get value() {
        return this.getAttribute('value') || '';
    }
    set value(text) {
        this.setAttribute('value', text);
    }
}
