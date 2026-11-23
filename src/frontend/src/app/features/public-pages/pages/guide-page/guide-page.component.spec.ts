import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';

import { GUIDE_PAGE_CONTENT } from '../../content/guide-pages.content';
import { GUIDE_PAGES } from '../../public-pages';
import { GuidePageComponent } from './guide-page.component';

const structuredData = (): Record<string, any>[] => {
  const script = document.getElementById('ld-page');
  return JSON.parse(script?.textContent ?? '{}')['@graph'] ?? [];
};

describe('GuidePageComponent', () => {
  afterEach(() => {
    TestBed.resetTestingModule();
    document.getElementById('ld-page')?.remove();
  });

  const render = async (slug: string) => {
    await TestBed.configureTestingModule({
      imports: [GuidePageComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { data: { slug } } } },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(GuidePageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  };

  for (const entry of GUIDE_PAGES) {
    describe(entry.slug, () => {
      const content = GUIDE_PAGE_CONTENT[entry.slug];

      it('should answer the question in the first paragraph after the headline', async () => {
        const element = await render(entry.slug);

        expect(element.querySelectorAll('h1').length).toBe(1);
        expect(element.querySelector('h1')?.textContent?.trim()).toBe(content.headline);

        const firstParagraph = element.querySelector('h1 + p');
        expect(firstParagraph?.classList).toContain('guide-answer');
        expect(firstParagraph?.textContent?.trim()).toBe(content.answer);
      });

      it('should render every section with its own heading', async () => {
        const element = await render(entry.slug);

        const headings = Array.from(element.querySelectorAll('.guide-section h2')).map((heading) =>
          heading.textContent?.trim(),
        );
        expect(headings).toEqual(content.sections.map((section) => section.title));
        expect(element.querySelector('a.btn-primary[href="/cadastro"]')).not.toBeNull();
      });

      it('should publish article, breadcrumb and FAQ structured data', async () => {
        await render(entry.slug);

        const graph = structuredData();
        expect(graph.map((node) => node['@type'])).toEqual([
          'Article',
          'BreadcrumbList',
          'FAQPage',
        ]);
        expect(graph[0]['headline']).toBe(content.headline);
        expect(graph[0]['dateModified']).toBe(content.updatedAt);

        const breadcrumb = graph[1] as { itemListElement: { name: string; item: string }[] };
        expect(breadcrumb.itemListElement.map((item) => item.name)).toEqual([
          'Início',
          entry.label,
        ]);
        expect(breadcrumb.itemListElement[1].item.endsWith(`/${entry.slug}`)).toBe(true);
      });
    });
  }

  it('should offer the spreadsheet template only on the spreadsheet guide', async () => {
    const element = await render('planilha-de-cotacao-de-compras');

    const download = element.querySelector<HTMLAnchorElement>('.guide-download a');
    expect(download?.getAttribute('href')).toBe('/modelos/planilha-de-cotacao-de-compras.csv');
    expect(download?.hasAttribute('download')).toBe(true);

    TestBed.resetTestingModule();
    const other = await render('o-que-e-mapa-de-cotacao');
    expect(other.querySelector('.guide-download')).toBeNull();
  });

  it('should not repeat the direct answer between guides', () => {
    const answers = Object.values(GUIDE_PAGE_CONTENT).map((page) => page.answer);
    expect(new Set(answers).size).toBe(answers.length);
  });

  it('should not mention paid plans', () => {
    const text = JSON.stringify(GUIDE_PAGE_CONTENT);
    expect(text).not.toMatch(/\bPro\b|plano pago|assinatura|teste grátis|por mês/i);
  });
});
