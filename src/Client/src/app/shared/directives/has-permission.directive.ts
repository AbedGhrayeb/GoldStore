import { Directive, TemplateRef, ViewContainerRef, effect, inject, input } from '@angular/core';
import { AuthStore } from '../../core/auth/auth-store';

/**
 * Structural directive to show/hide content based on permissions.
 * Usage: *appHasPermission="'sales.manage'" or *appHasPermission="['sales.view','sales.manage']"
 * Also supports `appHasPermissionElse` template.
 */
@Directive({
  selector: '[appHasPermission]',
  standalone: true,
})
export class HasPermissionDirective {
  private readonly auth = inject(AuthStore);
  private readonly template = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);

  readonly appHasPermission = input.required<string | string[]>();
  readonly appHasPermissionElse = input<TemplateRef<unknown> | null>(null);

  constructor() {
    effect(() => {
      const required = this.appHasPermission();
      const perms = Array.isArray(required) ? required : [required];
      const has = perms.some((p) => this.auth.hasPermission(p)) || this.auth.hasRole('store_admin');
      this.vcr.clear();
      if (has) {
        this.vcr.createEmbeddedView(this.template);
      } else if (this.appHasPermissionElse()) {
        this.vcr.createEmbeddedView(this.appHasPermissionElse()!);
      }
    });
  }
}

@Directive({
  selector: '[appHasRole]',
  standalone: true,
})
export class HasRoleDirective {
  private readonly auth = inject(AuthStore);
  private readonly template = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);

  readonly appHasRole = input.required<string | string[]>();

  constructor() {
    effect(() => {
      const required = this.appHasRole();
      const roles = Array.isArray(required) ? required : [required];
      const has = roles.some((r) => this.auth.hasRole(r));
      this.vcr.clear();
      if (has) this.vcr.createEmbeddedView(this.template);
    });
  }
}
