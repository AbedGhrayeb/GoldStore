import { Component, inject, signal, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { FormSectionComponent } from '../../../../shared/components/form-section/form-section.component';
import { InventoryService } from '../../services/inventory.service';

@Component({
  selector: 'app-inventory-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, FormSectionComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header [title]="isEdit() ? 'تعديل عنصر' : 'إضافة عنصر جديد'" subtitle="إدارة بيانات عنصر المخزون">
      <button routerLink="/inventory" class="px-4 py-2.5 border border-gold-border/40 text-text-primary rounded-lg text-sm font-medium hover:bg-surface-hover transition-colors">عودة</button>
    </app-page-header>
    <div class="p-6">
      <form [formGroup]="itemForm" (ngSubmit)="onSubmit()" class="max-w-2xl space-y-6">
        <app-form-section title="معلومات العنصر">
          <div>
            <label class="block text-sm font-medium text-text-primary mb-1.5">الاسم</label>
            <input type="text" formControlName="name" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none focus:shadow-[0_0_0_3px_rgba(212,175,55,0.25)] transition-[border-color,box-shadow] duration-200" />
          </div>
          <div>
            <label class="block text-sm font-medium text-text-primary mb-1.5">الفئة</label>
            <select formControlName="category" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none transition-[border-color,box-shadow] duration-200">
              <option value="">اختر الفئة</option>
              <option value="ذهب">ذهب</option>
              <option value="مجوهرات">مجوهرات</option>
              <option value="سبائك">سبائك</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-text-primary mb-1.5">العيار</label>
            <select formControlName="karat" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none transition-[border-color,box-shadow] duration-200">
              <option value="">اختر العيار</option>
              <option value="24">24</option>
              <option value="22">22</option>
              <option value="21">21</option>
              <option value="18">18</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-text-primary mb-1.5">الوصف</label>
            <textarea formControlName="description" rows="3" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none resize-none transition-[border-color,box-shadow] duration-200"></textarea>
          </div>
        </app-form-section>
        <app-form-section title="التسعير والكمية">
          <div>
            <label class="block text-sm font-medium text-text-primary mb-1.5">الوزن (جرام)</label>
            <input type="number" formControlName="weight" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none transition-[border-color,box-shadow] duration-200" />
          </div>
          <div>
            <label class="block text-sm font-medium text-text-primary mb-1.5">الكمية</label>
            <input type="number" formControlName="quantity" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none transition-[border-color,box-shadow] duration-200" />
          </div>
          <div>
            <label class="block text-sm font-medium text-text-primary mb-1.5">سعر التكلفة</label>
            <input type="number" formControlName="costPrice" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none transition-[border-color,box-shadow] duration-200" />
          </div>
          <div>
            <label class="block text-sm font-medium text-text-primary mb-1.5">سعر البيع</label>
            <input type="number" formControlName="sellingPrice" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none transition-[border-color,box-shadow] duration-200" />
          </div>
        </app-form-section>
        <div class="flex justify-end gap-3">
          <button type="button" routerLink="/inventory" class="px-6 py-2.5 text-sm font-medium text-text-secondary bg-surface-hover rounded-lg hover:bg-gray-200 transition-colors">إلغاء</button>
          <button type="submit" [disabled]="itemForm.invalid" class="px-6 py-2.5 bg-gold-primary text-deep-black font-semibold text-sm rounded-lg hover:bg-gold-primary-hover transition-[background-color,opacity] duration-200 disabled:opacity-50 disabled:cursor-not-allowed">{{ isEdit() ? 'حفظ التغييرات' : 'إضافة' }}</button>
        </div>
      </form>
    </div>
  `,
})
export class InventoryFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly inventoryService = inject(InventoryService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly isEdit = signal(false);
  protected readonly itemForm = this.fb.group({
    name: ['', Validators.required],
    category: ['', Validators.required],
    karat: ['', Validators.required],
    description: [''],
    weight: [0, [Validators.required, Validators.min(0.1)]],
    quantity: [0, [Validators.required, Validators.min(1)]],
    costPrice: [0, [Validators.required, Validators.min(0)]],
    sellingPrice: [0, [Validators.required, Validators.min(0)]],
  });

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit.set(true);
      this.inventoryService.getItem(id).subscribe(item => this.itemForm.patchValue(item));
    }
  }

  protected onSubmit() {
    if (this.itemForm.invalid) return;
    const obs = this.isEdit()
      ? this.inventoryService.updateItem(this.route.snapshot.paramMap.get('id')!, this.itemForm.value as any)
      : this.inventoryService.createItem(this.itemForm.value as any);
    obs.subscribe(() => this.router.navigate(['/inventory']));
  }
}
