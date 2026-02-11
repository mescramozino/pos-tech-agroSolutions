import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PropertiesService } from '../../services/properties.service';

@Component({
  selector: 'app-property-form',
  standalone: true,
  imports: [RouterLink, FormsModule],
  template: `
    <div class="toolbar">
      <a routerLink="/properties" class="btn btn-secondary">← Voltar</a>
    </div>
    <div class="card" style="max-width: 480px;">
      <h1>Nova propriedade</h1>
      @if (error) { <p class="status-drought">{{ error }}</p> }
      <form (ngSubmit)="submit()">
        <div class="form-group">
          <label>Nome</label>
          <input [(ngModel)]="name" name="name" required />
        </div>
        <div class="form-group">
          <label>Localização</label>
          <input [(ngModel)]="location" name="location" />
        </div>
        <button type="submit" class="btn btn-primary" [disabled]="loading">Criar</button>
      </form>
    </div>
  `,
})
export class PropertyFormComponent {
  name = '';
  location = '';
  loading = false;
  error = '';

  constructor(
    private props: PropertiesService,
    private router: Router,
  ) {}

  submit() {
    this.error = '';
    this.loading = true;
    this.props.createProperty({ name: this.name, location: this.location || null }).subscribe({
      next: (p) => this.router.navigate(['/properties', p.id]),
      error: () => {
        this.loading = false;
        this.error = 'Erro ao criar propriedade.';
      },
      complete: () => (this.loading = false),
    });
  }
}
