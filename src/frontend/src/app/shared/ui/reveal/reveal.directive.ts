import { DestroyRef, Directive, ElementRef, afterNextRender, inject } from '@angular/core';

/**
 * Faz o elemento surgir (sobe e ganha opacidade) quando entra na tela pela primeira vez.
 * A classe que esconde só é aplicada se o navegador tiver IntersectionObserver e a pessoa
 * não pedir menos movimento, então sem JavaScript ou em teste o conteúdo fica sempre visível.
 */
@Directive({
  selector: '[appReveal]',
})
export class RevealDirective {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private readonly destroyRef = inject(DestroyRef);

  constructor() {
    afterNextRender(() => {
      const browserWindow = this.element.ownerDocument.defaultView;
      const prefersReducedMotion =
        typeof browserWindow?.matchMedia === 'function' &&
        browserWindow.matchMedia('(prefers-reduced-motion: reduce)').matches;

      if (!browserWindow || !('IntersectionObserver' in browserWindow) || prefersReducedMotion) {
        return;
      }

      // Já visível ao abrir a página: não esconde, para não piscar.
      if (this.element.getBoundingClientRect().top < browserWindow.innerHeight) {
        return;
      }

      this.element.classList.add('reveal');
      const observer = new IntersectionObserver(
        (entries) => {
          if (entries.some((entry) => entry.isIntersecting)) {
            this.element.classList.add('is-visible');
            observer.disconnect();
          }
        },
        { rootMargin: '0px 0px -12% 0px' },
      );
      observer.observe(this.element);
      this.destroyRef.onDestroy(() => observer.disconnect());
    });
  }
}
