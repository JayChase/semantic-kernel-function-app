import { Platform } from '@angular/cdk/platform';
import { Directive, effect, ElementRef, inject, input } from '@angular/core';

@Directive({
  selector: '[appScrollHere]'
})
export class ScrollHere {
  appScrollHere = input.required<boolean>();
  scrollOptions = input<ScrollIntoViewOptions>({
    behavior: 'smooth',
    block: 'end',
    inline: 'start'
  });

  private elementRef = inject(ElementRef);
  private platform = inject(Platform);

  constructor() {
    effect(() => {
      if (this.platform.isBrowser && this.appScrollHere()) {
        (this.elementRef.nativeElement as HTMLInputElement).scrollIntoView(
          this.scrollOptions()
        );
      }
    });
  }
}
