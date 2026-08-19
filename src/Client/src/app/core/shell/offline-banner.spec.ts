import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { OfflineBanner } from './offline-banner';
import { OfflineStore } from '../offline/offline-store';

describe('OfflineBanner', () => {
  let store: OfflineStore;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    store = TestBed.inject(OfflineStore);
  });

  afterEach(() => {
    window.dispatchEvent(new Event('online'));
  });

  it('is hidden while online', () => {
    const fixture = TestBed.createComponent(OfflineBanner);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
  });

  it('appears when the network drops', () => {
    window.dispatchEvent(new Event('offline'));
    const fixture = TestBed.createComponent(OfflineBanner);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('disappears when the connection returns', () => {
    window.dispatchEvent(new Event('offline'));
    const fixture = TestBed.createComponent(OfflineBanner);
    fixture.detectChanges();
    window.dispatchEvent(new Event('online'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
  });

  it('retry button re-checks connectivity', () => {
    window.dispatchEvent(new Event('offline'));
    const fixture = TestBed.createComponent(OfflineBanner);
    fixture.detectChanges();
    const checkNow = vi.spyOn(store, 'checkNow').mockResolvedValue(true);
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    button.click();
    expect(checkNow).toHaveBeenCalledTimes(1);
  });
});
