import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from '../stores/auth.stores';

export const authGuard: CanActivateFn = () => {
  const store = inject(AuthStore);
  const router = inject(Router);

  if (store.isAuthenticated()) return true;

  router.navigate(['/auth/login']);
  return false;
};
