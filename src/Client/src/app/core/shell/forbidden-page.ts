import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideAngularModule, ShieldX } from 'lucide-angular';

@Component({
  selector: 'app-forbidden-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LucideAngularModule],
  template: `
    <div class="mx-auto flex max-w-lg flex-col items-center rounded-xl bg-card p-10 text-center shadow-sm">
      <span class="flex h-16 w-16 items-center justify-center rounded-full bg-error/10">
        <lucide-icon [img]="icon" [size]="28" class="text-error" />
      </span>
      <h1 class="mt-4 text-2xl font-bold text-gray-800">غير مصرح</h1>
      <p class="mt-2 text-sm leading-6 text-gray-500">
        ليس لديك صلاحية للوصول إلى هذه الصفحة. تواصل مع مدير المتجر إذا كنت تعتقد أن هذا خطأ.
      </p>
      <a routerLink="/" class="mt-6 rounded-full bg-gold px-6 py-2.5 text-sm font-bold text-white shadow-sm hover:bg-[#b8962e]">
        العودة للرئيسية
      </a>
    </div>
  `,
})
export class ForbiddenPage {
  readonly icon = ShieldX;
}
