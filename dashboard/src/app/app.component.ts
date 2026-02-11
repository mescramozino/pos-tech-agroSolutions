import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { AuthService } from './core/auth.service';
import { WeatherWidgetComponent } from './components/weather-widget/weather-widget.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, WeatherWidgetComponent],
  template: `
    @if (auth.isLoggedIn()) {
      <nav>
        <a routerLink="/properties">Propriedades</a>
        <a routerLink="/alerts">Alertas</a>
        <span style="margin-left: auto;">{{ auth.getEmail() }}</span>
        <button class="btn btn-secondary" (click)="logout()">Sair</button>
      </nav>
    }
    <main class="container">
      @if (auth.isLoggedIn()) {
        <aside class="weather-aside">
          <app-weather-widget />
        </aside>
      }
      <div class="main-content">
        <router-outlet />
      </div>
    </main>
  `,
  styles: [`
    .container { display: flex; gap: 1.5rem; align-items: flex-start; }
    .weather-aside { flex-shrink: 0; }
    .main-content { flex: 1; min-width: 0; }
    @media (max-width: 768px) { .container { flex-direction: column; } }
  `],
})
export class AppComponent {
  constructor(public auth: AuthService) {}
  logout() {
    this.auth.logout();
    window.location.href = '/login';
  }
}
