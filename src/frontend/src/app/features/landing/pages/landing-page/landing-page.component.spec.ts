import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { LandingPageComponent } from './landing-page.component';

describe('LandingPageComponent', () => {
  let element: HTMLElement;
  let fixture: ComponentFixture<LandingPageComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LandingPageComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(LandingPageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    element = fixture.nativeElement as HTMLElement;
  });

  it('should render a single headline aimed at retail buyers', () => {
    const headlines = element.querySelectorAll('h1');

    expect(headlines.length).toBe(1);
    expect(headlines[0].textContent).toContain('Planeje suas compras');
  });

  it('should expose the sections the header links to', () => {
    for (const id of ['como-funciona', 'ia', 'recursos', 'precos', 'duvidas']) {
      expect(element.querySelector(`#${id}`)).not.toBeNull();
    }
  });

  it('should send every call to action to registration', () => {
    const primaryActions = Array.from(element.querySelectorAll('a.btn-primary'));

    expect(primaryActions.length).toBeGreaterThan(0);
    for (const action of primaryActions) {
      expect(action.getAttribute('href')).toBe('/cadastro');
    }
  });

  it('should keep the demo totals consistent with the demo rows', () => {
    const quantities = [40, 60, 30, 48];
    const rows = Array.from(element.querySelectorAll('.hero-mock tbody tr'));
    const totals = [0, 0, 0];

    rows.forEach((row, rowIndex) => {
      Array.from(row.querySelectorAll('td'))
        .slice(1)
        .forEach((cell, supplierIndex) => {
          const unitPrice = Number(
            (cell.textContent ?? '').replace('R$', '').trim().replace(',', '.'),
          );
          totals[supplierIndex] += unitPrice * quantities[rowIndex];
        });
    });

    const shown = Array.from(element.querySelectorAll('.hero-mock tfoot td')).map((cell) =>
      Number((cell.textContent ?? '').replace('R$', '').trim().replace('.', '').replace(',', '.')),
    );

    expect(totals.map((total) => Number(total.toFixed(2)))).toEqual(shown);
  });

  it('should switch the demo screen from the mock sidebar', async () => {
    const buttons = Array.from(element.querySelectorAll<HTMLButtonElement>('.hero-mock aside button'));
    const orders = buttons.find((button) => button.textContent?.includes('Pedidos'));

    expect(buttons.length).toBe(8);
    expect(element.querySelector('.hero-mock')?.textContent).toContain('Total por fornecedor');

    orders?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const mock = element.querySelector('.hero-mock');
    expect(orders?.getAttribute('aria-pressed')).toBe('true');
    expect(mock?.textContent).toContain('Pedidos emitidos');
    expect(mock?.textContent).not.toContain('Total por fornecedor');
  });

  it('should redraw the dashboard chart when the period filter changes', async () => {
    const open = (label: string) =>
      Array.from(element.querySelectorAll<HTMLButtonElement>('.hero-mock button'))
        .find((button) => button.textContent?.trim() === label)
        ?.click();

    open('Dashboard');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(element.querySelector('.hero-mock figcaption')?.textContent).toContain('Pedidos por semana');
    expect(element.querySelectorAll('.chart-dot').length).toBe(4);

    open('12 meses');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(element.querySelector('.hero-mock figcaption')?.textContent).toContain('Pedidos por mês');
    expect(element.querySelectorAll('.chart-dot').length).toBe(12);
    expect(element.querySelector('.hero-mock')?.textContent).toContain('R$ 48.730,00');

    open('Listas');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(element.querySelector('.hero-mock figcaption')?.textContent).toContain('Listas por mês');
  });

  it('should keep every section visible when scroll reveal is not available', () => {
    // Sem IntersectionObserver (como aqui), nada pode ficar escondido esperando a rolagem.
    expect(element.querySelectorAll('section.reveal').length).toBe(0);
    expect(element.querySelectorAll('section').length).toBeGreaterThan(5);
  });
});
