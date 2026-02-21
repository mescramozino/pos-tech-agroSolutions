import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { IdentityService } from '../../services/identity.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css'],
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
      next: () => this.router.navigate(['/painel']),
      error: (err) => {
        this.loading = false;
        this.error = err.status === 400 ? 'E-mail já cadastrado ou inválido.' : 'Erro ao cadastrar.';
      },
      complete: () => (this.loading = false),
    });
  }
}
