import { Component } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { NavbarComponent } from './components/navbar/navbar.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
})
export class AppComponent {
  sidebarOpen = true;

  constructor(
    public auth: AuthService,
    private router: Router,
  ) {}

  get pageTitle(): string {
    const url = this.router.url;
    if (url === '/painel') return 'Dashboard';
    if (url.startsWith('/properties/new')) return 'Nova propriedade';
    if (url.startsWith('/properties/') && url !== '/properties') return 'Propriedade';
    if (url === '/properties') return 'Propriedades';
    if (url.startsWith('/plots/')) return 'Talhão';
    if (url === '/plots') return 'Talhões';
    if (url === '/alerts') return 'Alertas';
    if (url === '/users/me') return 'Minha conta';
    if (url === '/register') return 'Criar usuário';
    return 'Dashboard';
  }
}
