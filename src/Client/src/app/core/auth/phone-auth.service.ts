import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AuthApi } from './auth-api.service';

/**
 * Firebase Phone Auth — real SDK (no dev-token fallback when Firebase configured).
 * dev-token:+970... still works via LogPhoneVerifier when Firebase not configured or AllowDevTokens=true.
 */
@Injectable({ providedIn: 'root' })
export class PhoneAuthService {
  private readonly authApi = inject(AuthApi);
  private firebaseApp: unknown = null;
  private authInstance: unknown = null;

  async ensureFirebase(isHost = false): Promise<boolean> {
    try {
      const cfg = await firstValueFrom(this.authApi.getFirebaseConfig(isHost));
      if (!cfg.projectId || !cfg.webApiKey) return false;

      const { initializeApp, getApps } = await import('firebase/app');
      const { getAuth } = await import('firebase/auth');
      const apps = getApps();
      const app =
        apps.length === 0
          ? initializeApp({ apiKey: cfg.webApiKey, authDomain: cfg.authDomain, projectId: cfg.projectId, appId: cfg.appId })
          : apps[0];
      this.firebaseApp = app;
      this.authInstance = getAuth(app);
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Sends SMS via Firebase and returns idToken.
   * When Firebase not configured (cfg empty or import fails) falls back to dev-token:+970...
   * which LogPhoneVerifier accepts in Development/AllowDevTokens.
   */
  async sendCode(phoneE164: string, isHost = false): Promise<string> {
    const hasFirebase = await this.ensureFirebase(isHost);

    if (hasFirebase && this.authInstance) {
      const { RecaptchaVerifier, signInWithPhoneNumber } = await import('firebase/auth');

      let container = document.getElementById('recaptcha-container');
      if (!container) {
        container = document.createElement('div');
        container.id = 'recaptcha-container';
        document.body.appendChild(container);
      }
      const verifier = new RecaptchaVerifier(this.authInstance as import('firebase/auth').Auth, 'recaptcha-container', {
        size: 'invisible',
      });
      const result = await signInWithPhoneNumber(
        this.authInstance as import('firebase/auth').Auth,
        phoneE164,
        verifier,
      );
      const code = window.prompt(`أدخل رمز التحقق المرسل إلى ${phoneE164}`) ?? '';
      const cred = await result.confirm(code);
      const idToken = await cred.user.getIdToken();
      return idToken;
    }

    // Dev fallback: dev-token
    if (!phoneE164.startsWith('+970') && !phoneE164.startsWith('+')) {
      if (phoneE164.startsWith('0')) phoneE164 = '+970' + phoneE164.slice(1);
      else phoneE164 = '+970' + phoneE164;
    }
    return `dev-token:${phoneE164}`;
  }

  async getIdTokenForPhone(phoneE164: string, isHost = false): Promise<string> {
    return this.sendCode(phoneE164, isHost);
  }
}
