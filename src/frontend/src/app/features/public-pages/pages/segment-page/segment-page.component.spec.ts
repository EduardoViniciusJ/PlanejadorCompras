import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';

import { SEGMENT_PAGE_CONTENT } from '../../content/segment-pages.content';
import { buildPriceMapExample } from '../../models/price-map-example';
import { SEGMENT_PAGES } from '../../public-pages';
import { SegmentPageComponent } from './segment-page.component';

/** Grafo JSON-LD publicado pela página no `<head>`. */
const structuredData = (): Record<string, any>[] => {
  const script = document.getElementById('ld-page');
  return JSON.parse(script?.textContent ?? '{}')['@graph'] ?? [];
};

describe('SegmentPageComponent', () => {
  afterEach(() => {
    TestBed.resetTestingModule();
    document.getElementById('ld-page')?.remove();
  });

  const render = async (slug: string) => {
    await TestBed.configureTestingModule({
      imports: [SegmentPageComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { data: { slug } } } },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(SegmentPageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  };

  for (const entry of SEGMENT_PAGES) {
    describe(entry.slug, () => {
      const content = SEGMENT_PAGE_CONTENT[entry.slug];

      it('should render the segment content with one headline', async () => {
        const { element } = await render(entry.slug);

        const headlines = element.querySelectorAll('h1');
        expect(headlines.length).toBe(1);
        expect(headlines[0].textContent?.trim()).toBe(content.headline);
        expect(element.querySelector('.segment-lead')?.textContent?.trim()).toBe(content.lead);
        expect(element.querySelector('[aria-current="page"]')?.textContent?.trim()).toBe(
          entry.label,
        );
        expect(element.querySelectorAll('.segment-pains li').length).toBe(content.pains.length);
      });

      it('should show the four steps of the flow in order', async () => {
        const { element } = await render(entry.slug);

        const steps = Array.from(element.querySelectorAll('.segment-flow li h3')).map((step) =>
          step.textContent?.trim(),
        );
        expect(steps).toEqual(content.flow.map((step) => step.title));
      });

      it('should highlight the lowest price of every row and show consistent totals', async () => {
        const { element } = await render(entry.slug);
        const expected = buildPriceMapExample(content.example);

        const rows = element.querySelectorAll('.segment-example tbody tr');
        expect(rows.length).toBe(content.example.rows.length);
        rows.forEach((row) =>
          expect(row.querySelectorAll('.badge-success').length).toBeGreaterThan(0),
        );

        const totals = Array.from(element.querySelectorAll('.segment-example tfoot td')).map(
          (cell) => cell.textContent?.trim(),
        );
        expect(totals).toEqual(expected.totals.map((total) => `R$ ${total}`));
      });

      it('should end with the call to create an account', async () => {
        const { element } = await render(entry.slug);

        const actions = Array.from(element.querySelectorAll('a.btn-primary'));
        expect(actions.length).toBeGreaterThan(0);
        for (const action of actions) {
          expect(action.getAttribute('href')).toBe('/cadastro');
        }
        // Links para as outras páginas, sem apontar para a própria.
        const related = Array.from(element.querySelectorAll('app-page-closing nav a')).map((link) =>
          link.getAttribute('href'),
        );
        expect(related).not.toContain(`/${entry.slug}`);
        expect(related.length).toBe(SEGMENT_PAGES.length - 1 + 3);
      });

      it('should publish breadcrumb, page and FAQ structured data', async () => {
        await render(entry.slug);

        const graph = structuredData();
        expect(graph.map((node) => node['@type'])).toEqual([
          'WebPage',
          'BreadcrumbList',
          'FAQPage',
        ]);

        const breadcrumb = graph[1] as { itemListElement: { name: string; item: string }[] };
        expect(breadcrumb.itemListElement.map((item) => item.name)).toEqual([
          'Início',
          entry.label,
        ]);
        expect(breadcrumb.itemListElement[1].item.endsWith(`/${entry.slug}`)).toBe(true);

        const faq = graph[2] as { mainEntity: { name: string }[] };
        expect(faq.mainEntity.map((question) => question.name)).toEqual(
          content.questions.map((question) => question.question),
        );
      });
    });
  }

  it('should remove its structured data when the page is closed', async () => {
    const { fixture } = await render('para-mercados');
    expect(document.getElementById('ld-page')).not.toBeNull();

    fixture.destroy();
    expect(document.getElementById('ld-page')).toBeNull();
  });

  it('should not reuse the same headline, lead or example between segments', () => {
    const pages = Object.values(SEGMENT_PAGE_CONTENT);
    for (const field of ['headline', 'lead', 'painsTitle', 'flowTitle', 'closingTitle'] as const) {
      expect(new Set(pages.map((page) => page[field])).size).toBe(pages.length);
    }
    const items = pages.flatMap((page) => page.example.rows.map((row) => row.item));
    expect(new Set(items).size).toBe(items.length);
  });

  it('should not mention paid plans', () => {
    const text = JSON.stringify(SEGMENT_PAGE_CONTENT);
    expect(text).not.toMatch(/\bPro\b|plano|assinatura|teste grátis|por mês/i);
  });
});
