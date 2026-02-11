import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { IdentityService } from '../../services/identity.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <div class="card" style="max-width: 400px; margin: 2rem auto;">
      <h1>Criar conta</h1>
      @if (error) { <p class="status-drought">{{ error }}</p> }
      <form (ngSubmit)="register()">
        <div class="form-group">
          <label>E-mail</label>
          <input type="email" [(ngModel)]="email" name="email" required />
        </div>
        <div class="form-group">
          <label>Senha</label>
          <input type="password" [(ngModel)]="password" name="password" required />
        </div>
        <button type="submit" class="btn btn-primary" [disabled]="loading">Cadastrar</button>
      </form>
      <p style="margin-top: 1rem;"><a routerLink="/login">Já tenho conta</a></p>
    </div>
  `,
})
export class RegisterComponent {
  email = '';
  password = '';
  error = '';
  loading = false;

  constructor(private identity: IdentityService, private router: Router) {}

  register() {
    this.error = '';
    this.loading = true;
    this.identity.register({ email: this.email, password: this.password }).subscribe({
      next: () => this.router.navigate(['/properties']),
      error: (err) => {
        this.loading = false;
        this.error = err.status === 400 ? 'E-mail já cadastrado ou inválido.' : 'Erro ao cadastrar.';
      },
      complete: () => (this.loading = false),
    });
  }
}
