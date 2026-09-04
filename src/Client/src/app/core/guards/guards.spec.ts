import { Component, computed, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import type { MeResponse } from '../../shared/api/api-types';
import { AuthStore } from '../auth/auth-store';
import { authGuard } from './auth.guard';
import { featureGuard } from './feature.guard';
import { hostGuard } from './host.guard';

@Component({ template: '' })
class DummyComponent {}

const ME: MeResponse = {
  userId: 'u-1',
  email: 'owner@store.test',
  tenantId: 't-1',
  tenantKey: 'gold1',
  roles: ['Owner'],
  permissions: ['catalog', 'sales'],
};

function fakeAuthStore(user: MeResponse | null, hostAdmin = false): AuthStore {
  const userSignal = signal(user);
  const hostAdminSignal = signal(hostAdmin);
  return {
    user: userSignal.asReadonly(),
    isAuthenticated: computed(() => userSignal() !== null),
    permissions: computed(() => userSignal()?.permissions ?? []),
    isHostAdmin: hostAdminSignal.asReadonly(),
  } as unknown as AuthStore;
}

function configureRouter(auth: AuthStore): Router {
  TestBed.configureTestingModule({
    providers: [
      { provide: AuthStore, useValue: auth },
      provideRouter([
        { path: 'login', component: DummyComponent },
        { path: '403', component: DummyComponent },
        { path: 'host/login', component: DummyComponent },
        { path: 'dashboard', component: DummyComponent, canActivate: [authGuard] },
        { path: 'catalog', component: DummyComponent, canActivate: [featureGuard('catalog')] },
        { path: 'sales', component: DummyComponent, canActivate: [featureGuard('sales')] },
        { path: 'host', component: DummyComponent, canActivate: [hostGuard] },
      ]),
    ],
  });
  return TestBed.inject(Router);
}

describe('guards', () => {
  it('authGuard redirects to /login with returnUrl when not authenticated', async () => {
    const router = configureRouter(fakeAuthStore(null));
    await RouterTestingHarness.create();
    await router.navigateByUrl('/dashboard');
    expect(router.url).toBe('/login?returnUrl=%2Fdashboard');
  });

  it('authGuard allows authenticated users through', async () => {
    const router = configureRouter(fakeAuthStore(ME));
    await RouterTestingHarness.create();
    await router.navigateByUrl('/dashboard');
    expect(router.url).toBe('/dashboard');
  });

  it('featureGuard redirects to /403 when the claim is absent', async () => {
    const router = configureRouter(fakeAuthStore({ ...ME, permissions: ['sales'] }));
    await RouterTestingHarness.create();
    await router.navigateByUrl('/catalog');
    expect(router.url).toBe('/403');
  });

  it('featureGuard allows users holding the claim', async () => {
    const router = configureRouter(fakeAuthStore(ME));
    await RouterTestingHarness.create();
    await router.navigateByUrl('/sales');
    expect(router.url).toBe('/sales');
  });

  it('hostGuard redirects to /host/login for non-host users', async () => {
    const router = configureRouter(fakeAuthStore(ME, false));
    await RouterTestingHarness.create();
    await router.navigateByUrl('/host');
    expect(router.url).toBe('/host/login');
  });

  it('hostGuard allows host admins through', async () => {
    const router = configureRouter(fakeAuthStore(null, true));
    await RouterTestingHarness.create();
    await router.navigateByUrl('/host');
    expect(router.url).toBe('/host');
  });
});
