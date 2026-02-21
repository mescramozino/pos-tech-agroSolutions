import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { IdentityService } from '../../services/identity.service';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
})
export class LoginComponent {
  email = '';
  password = '';
  remember = false;
  error = '';
  loading = false;

  constructor(
    private identity: IdentityService,
    private auth: AuthService,
    private router: Router
  ) {
    if (this.auth.isLoggedIn()) this.router.navigate(['/painel']);
  }

  login() {
    this.error = '';
    this.loading = true;
    this.identity.login({ email: this.email, password: this.password }).subscribe({
      next: () => this.router.navigate(['/painel']),
      error: (err) => {
        this.loading = false;
        this.error = err.status === 401 ? 'E-mail ou senha inválidos.' : 'Erro ao entrar.';
      },
      complete: () => (this.loading = false),
    });
  }
}
