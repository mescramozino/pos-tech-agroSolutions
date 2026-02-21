import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css'],
})
export class NavbarComponent {
  @Input() open = true;
  @Output() openChange = new EventEmitter<boolean>();

  constructor(
    private auth: AuthService,
    private router: Router,
  ) {}

  onNavClick(): void {
    this.openChange.emit(false);
  }

  close(): void {
    this.openChange.emit(false);
  }

  isPropertiesPage(): boolean {
    return this.router.url.startsWith('/properties/');
  }

  logout(): void {
    this.auth.logout();
    window.location.href = '/login';
  }
}
