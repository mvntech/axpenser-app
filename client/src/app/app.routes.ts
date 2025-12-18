import { Routes } from '@angular/router';
import { LayoutPublic } from './layout/layout-public/layout-public';
import { AuthLayout } from './features/auth/auth';

export const routes: Routes = [
  {
    path: '',
    component: LayoutPublic,
    children: [
      {
        path: '',
        loadChildren: () =>
          import('./features/landing/landing.routes').then(m => m.LANDING_ROUTES),
      },
    ],
  },
  {
    path: 'auth',
    component: AuthLayout,
    loadChildren: () =>
      import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES),
  },
  { path: '**', redirectTo: '' },
];
