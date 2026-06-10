import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormArray, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { FormSectionComponent } from '../../../../shared/components/form-section/form-section.component';
import { SalesService } from '../../services/sales.service';

@Component({
  selector: 'app-create-sale',
  standalone: true,
  imports: [DecimalPipe, ReactiveFormsModule, RouterLink, PageHeaderComponent, FormSectionComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-header title="فاتورة مبيعات جديدة" subtitle="إدخال بيانات فاتورة المبيعات">
      <button routerLink="/sales" class="px-4 py-2.5 border border-gold-border/40 text-text-primary rounded-lg text-sm font-medium hover:bg-surface-hover transition-colors">عودة</button>
    </app-page-header>
    <div class="p-6">
      <form [formGroup]="saleForm" (ngSubmit)="onSubmit()" class="max-w-4xl space-y-6">
        <app-form-section title="معلومات العميل">
          <div>
            <label class="block text-sm font-medium text-text-primary mb-1.5">اسم العميل *</label>
            <input type="text" formControlName="customerName" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none transition-[border-color,box-shadow] duration-200" />
          </div>
          <div>
            <label class="block text-sm font-medium text-text-primary mb-1.5">رقم الهاتف</label>
            <input type="tel" formControlName="customerPhone" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none transition-[border-color,box-shadow] duration-200" />
          </div>
        </app-form-section>

        <div class="bg-surface-card rounded-xl border border-gold-border/30 shadow-card p-6">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-base font-semibold text-text-primary">عناصر الفاتورة</h3>
            <button type="button" (click)="addItem()" class="px-3 py-1.5 text-sm font-medium text-gold-primary bg-gold-primary/10 rounded-lg hover:bg-gold-primary/20 transition-colors">+ إضافة عنصر</button>
          </div>
          <div formArrayName="items" class="space-y-3">
            @for (item of items.controls; track $index) {
              <div [formGroupName]="$index" class="flex gap-2 items-start p-3 bg-surface-base/50 rounded-lg">
                <div class="flex-1"><input formControlName="productName" placeholder="اسم المنتج" class="w-full py-1.5 px-2 border border-gray-200 rounded text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none" /></div>
                <div class="w-20"><input formControlName="karat" placeholder="العيار" class="w-full py-1.5 px-2 border border-gray-200 rounded text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none" /></div>
                <div class="w-24"><input type="number" formControlName="weight" placeholder="الوزن" class="w-full py-1.5 px-2 border border-gray-200 rounded text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none" /></div>
                <div class="w-20"><input type="number" formControlName="quantity" placeholder="الكمية" class="w-full py-1.5 px-2 border border-gray-200 rounded text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none" /></div>
                <div class="w-28"><input type="number" formControlName="unitPrice" placeholder="سعر الوحدة" class="w-full py-1.5 px-2 border border-gray-200 rounded text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none" /></div>
                <div class="w-24 text-sm text-text-primary py-1.5 text-left pt-2" dir="ltr">{{ item.get('totalPrice')?.value | number }}</div>
                <button type="button" (click)="removeItem($index)" class="p-1.5 text-error hover:bg-error/5 rounded transition-colors">
                  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
              </div>
            }
          </div>
          <div class="flex justify-end mt-4 pt-4 border-t border-gold-border/20">
            <div class="text-left">
              <div class="flex justify-between gap-8 text-sm mb-1"><span class="text-text-muted">المجموع:</span><span class="text-text-primary font-medium" dir="ltr">{{ saleForm.get('totalAmount')?.value | number }} ر.س</span></div>
              <div class="flex justify-between gap-8 text-sm mb-1"><span class="text-text-muted">الخصم:</span><span class="text-text-primary font-medium" dir="ltr">{{ saleForm.get('discount')?.value | number }} ر.س</span></div>
              <div class="flex justify-between gap-8 text-base font-bold"><span class="text-text-primary">الإجمالي:</span><span class="text-gold-primary" dir="ltr">{{ saleForm.get('finalAmount')?.value | number }} ر.س</span></div>
            </div>
          </div>
        </div>

        <app-form-section title="الدفع">
          <div>
            <label class="block text-sm font-medium text-text-primary mb-1.5">الخصم</label>
            <input type="number" formControlName="discount" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none transition-[border-color,box-shadow] duration-200" />
          </div>
          <div>
            <label class="block text-sm font-medium text-text-primary mb-1.5">طريقة الدفع</label>
            <select formControlName="paymentMethod" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none transition-[border-color,box-shadow] duration-200">
              <option value="cash">نقدي</option>
              <option value="card">بطاقة</option>
              <option value="bank_transfer">تحويل بنكي</option>
              <option value="credit">آجل</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-text-primary mb-1.5">المبلغ المدفوع</label>
            <input type="number" formControlName="paidAmount" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none transition-[border-color,box-shadow] duration-200" />
          </div>
          <div>
            <label class="block text-sm font-medium text-text-primary mb-1.5">ملاحظات</label>
            <textarea formControlName="notes" rows="2" class="w-full py-2 px-3 border border-gray-200 rounded-lg text-sm text-text-primary bg-surface-card focus:border-gold-primary focus:outline-none resize-none transition-[border-color,box-shadow] duration-200"></textarea>
          </div>
        </app-form-section>
        <div class="flex justify-end gap-3">
          <button type="button" routerLink="/sales" class="px-6 py-2.5 text-sm font-medium text-text-secondary bg-surface-hover rounded-lg hover:bg-gray-200 transition-colors">إلغاء</button>
          <button type="submit" [disabled]="saleForm.invalid" class="px-6 py-2.5 bg-gold-primary text-deep-black font-semibold text-sm rounded-lg hover:bg-gold-primary-hover transition-[background-color,opacity] duration-200 disabled:opacity-50 disabled:cursor-not-allowed">إنشاء الفاتورة</button>
        </div>
      </form>
    </div>
  `,
})
export class CreateSaleComponent {
  private readonly fb = inject(FormBuilder);
  private readonly salesService = inject(SalesService);
  private readonly router = inject(Router);

  protected readonly saleForm = this.fb.group({
    customerName: ['', Validators.required],
    customerPhone: [''],
    items: this.fb.array([]),
    discount: [0],
    paymentMethod: ['cash'],
    paidAmount: [0],
    notes: [''],
    totalAmount: [0],
    finalAmount: [0],
  });

  get items(): FormArray { return this.saleForm.get('items') as FormArray; }

  protected addItem() {
    const itemGroup = this.fb.group({
      productName: ['', Validators.required],
      karat: [''],
      weight: [0, [Validators.required, Validators.min(0.1)]],
      quantity: [1, [Validators.required, Validators.min(1)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
      totalPrice: [{ value: 0, disabled: true }],
    });
    itemGroup.valueChanges.subscribe(v => {
      const total = (v.weight || 0) * (v.quantity || 0) * (v.unitPrice || 0);
      itemGroup.get('totalPrice')?.setValue(total, { emitEvent: false });
      this.recalculateTotals();
    });
    this.items.push(itemGroup);
  }

  protected removeItem(index: number) { this.items.removeAt(index); this.recalculateTotals(); }

  private recalculateTotals() {
    let total = 0;
    for (let i = 0; i < this.items.length; i++) {
      const item = this.items.at(i);
      total += (item.get('weight')?.value || 0) * (item.get('quantity')?.value || 0) * (item.get('unitPrice')?.value || 0);
    }
    const discount = this.saleForm.get('discount')?.value || 0;
    this.saleForm.get('totalAmount')?.setValue(total, { emitEvent: false });
    this.saleForm.get('finalAmount')?.setValue(total - discount, { emitEvent: false });
  }

  protected onSubmit() {
    if (this.saleForm.invalid) return;
    this.salesService.createInvoice(this.saleForm.value as any).subscribe(() => this.router.navigate(['/sales']));
  }
}
