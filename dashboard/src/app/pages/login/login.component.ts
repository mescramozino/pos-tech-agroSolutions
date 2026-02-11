import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { IdentityService } from '../../services/identity.service';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <div class="card" style="max-width: 400px; margin: 2rem auto;">
      <h1>Entrar</h1>
      @if (error) { <p class="status-drought">{{ error }}</p> }
      <form (ngSubmit)="login()">
        <div class="form-group">
          <label>E-mail</label>
          <input type="email" [(ngModel)]="email" name="email" required />
        </div>
        <div class="form-group">
          <label>Senha</label>
          <input type="password" [(ngModel)]="password" name="password" required />
        </div>
        <button type="submit" class="btn btn-primary" [disabled]="loading">Entrar</button>
      </form>
      <p style="margin-top: 1rem;"><a routerLink="/register">Criar conta</a></p>
    </div>
  `,
})
export class LoginComponent {
  email = '';
  password = '';
  error = '';
  loading = false;

  constructor(
    private identity: IdentityService,
    private auth: AuthService,
    private router: Router
  ) {
    if (this.auth.isLoggedIn()) this.router.navigate(['/properties']);
  }

  login() {
    this.error = '';
    this.loading = true;
    this.identity.login({ email: this.email, password: this.password }).subscribe({
      next: () => this.router.navigate(['/properties']),
      error: (err) => {
        this.loading = false;
        this.error = err.status === 401 ? 'E-mail ou senha inválidos.' : 'Erro ao entrar.';
      },
      complete: () => (this.loading = false),
    });
  }
}
