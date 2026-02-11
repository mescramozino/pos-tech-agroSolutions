import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'properties', pathMatch: 'full' },
  { path: 'login', loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./pages/register/register.component').then(m => m.RegisterComponent) },
  { path: 'properties', loadComponent: () => import('./pages/properties-list/properties-list.component').then(m => m.PropertiesListComponent), canActivate: [authGuard] },
  { path: 'properties/new', loadComponent: () => import('./pages/property-form/property-form.component').then(m => m.PropertyFormComponent), canActivate: [authGuard] },
  { path: 'properties/:id', loadComponent: () => import('./pages/property-detail/property-detail.component').then(m => m.PropertyDetailComponent), canActivate: [authGuard] },
  { path: 'plots/:id', loadComponent: () => import('./pages/plot-detail/plot-detail.component').then(m => m.PlotDetailComponent), canActivate: [authGuard] },
  { path: 'alerts', loadComponent: () => import('./pages/alerts-list/alerts-list.component').then(m => m.AlertsListComponent), canActivate: [authGuard] },
  { path: '**', redirectTo: 'properties' },
];
