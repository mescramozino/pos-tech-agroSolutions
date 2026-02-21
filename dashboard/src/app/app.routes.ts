import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'painel', pathMatch: 'full' },
  { path: 'painel', loadComponent: () => import('./pages/painel/painel.component').then(m => m.PainelComponent), canActivate: [authGuard] },
  { path: 'login', loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./pages/register/register.component').then(m => m.RegisterComponent) },
  { path: 'users/me', loadComponent: () => import('./pages/user-profile/user-profile.component').then(m => m.UserProfileComponent), canActivate: [authGuard] },
  { path: 'properties', loadComponent: () => import('./pages/properties-list/properties-list.component').then(m => m.PropertiesListComponent), canActivate: [authGuard] },
  { path: 'properties/new', loadComponent: () => import('./pages/property-form/property-form.component').then(m => m.PropertyFormComponent), canActivate: [authGuard] },
  { path: 'properties/:id', loadComponent: () => import('./pages/property-detail/property-detail.component').then(m => m.PropertyDetailComponent), canActivate: [authGuard] },
  { path: 'plots', loadComponent: () => import('./pages/plots-list/plots-list.component').then(m => m.PlotsListComponent), canActivate: [authGuard] },
  { path: 'plots/:id', loadComponent: () => import('./pages/plot-detail/plot-detail.component').then(m => m.PlotDetailComponent), canActivate: [authGuard] },
  { path: 'alerts', loadComponent: () => import('./pages/alerts-list/alerts-list.component').then(m => m.AlertsListComponent), canActivate: [authGuard] },
  { path: 'users', loadComponent: () => import('./pages/users-list/users-list.component').then(m => m.UsersListComponent), canActivate: [authGuard] },
  { path: '**', redirectTo: 'painel' },
];
