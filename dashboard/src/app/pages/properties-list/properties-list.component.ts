import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PropertiesService, Property } from '../../services/properties.service';

@Component({
  selector: 'app-properties-list',
  standalone: true,
  imports: [RouterLink],
  template: `
    <h1>Propriedades</h1>
    <a routerLink="/properties/new" class="btn btn-primary" style="margin-bottom: 1rem;">Nova propriedade</a>
    @if (loading) { <p>Carregando...</p> }
    @if (error) { <p class="status-drought">{{ error }}</p> }
    @for (p of properties; track p.id) {
      <div class="card">
        <h3><a [routerLink]="['/properties', p.id]">{{ p.name }}</a></h3>
        @if (p.location) { <p>{{ p.location }}</p> }
        <a [routerLink]="['/properties', p.id]">Ver talhões</a>
      </div>
    }
    @if (!loading && !error && properties.length === 0) {
      <p>Nenhuma propriedade. Crie uma para começar.</p>
    }
  `,
})
export class PropertiesListComponent implements OnInit {
  properties: Property[] = [];
  loading = false;
  error = '';

  constructor(private props: PropertiesService) {}

  ngOnInit() {
    this.loading = true;
    this.props.getProperties().subscribe({
      next: (list) => (this.properties = list),
      error: () => (this.error = 'Erro ao carregar propriedades.'),
      complete: () => (this.loading = false),
    });
  }
}
