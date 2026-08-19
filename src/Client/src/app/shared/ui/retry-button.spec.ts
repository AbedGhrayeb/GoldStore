import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RetryButton } from './retry-button';

@Component({
  imports: [RetryButton],
  template: ` <app-retry-button [loading]="loading()" [label]="label()" (retry)="onRetry()" /> `,
})
class HostComponent {
  loading = signal(false);
  label = signal('إعادة المحاولة');
  retries = 0;

  onRetry(): void {
    this.retries += 1;
  }
}

function createFixture(): ComponentFixture<HostComponent> {
  const fixture = TestBed.createComponent(HostComponent);
  fixture.detectChanges();
  return fixture;
}

describe('RetryButton', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('emits retry on click', () => {
    const fixture = createFixture();
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    button.click();
    expect(fixture.componentInstance.retries).toBe(1);
  });

  it('renders the label', () => {
    const fixture = createFixture();
    expect(fixture.nativeElement.textContent).toContain('إعادة المحاولة');
  });

  it('disables while loading', () => {
    const fixture = createFixture();
    fixture.componentInstance.loading.set(true);
    fixture.detectChanges();
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
  });
});
