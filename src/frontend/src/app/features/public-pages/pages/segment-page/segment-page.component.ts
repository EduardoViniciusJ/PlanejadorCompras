import { ChangeDetectionStrategy, Component, DestroyRef, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { SeoService } from '../../../../core/seo/seo.service';
import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { PageBreadcrumbComponent } from '../../components/page-breadcrumb/page-breadcrumb.component';
import { PageClosingComponent } from '../../components/page-closing/page-closing.component';
import { SEGMENT_PAGE_CONTENT } from '../../content/segment-pages.content';
import { buildPriceMapExample } from '../../models/price-map-example';
import { SOFTWARE_APPLICATION, breadcrumbList, faqPage } from '../../models/structured-data';
import { PublicPageRouteData, SEGMENT_PAGES, SegmentPageSlug } from '../../public-pages';

/** Ícone de cada passo do fluxo: lista, cotação, mapa de preços e pedido. */
const FLOW_ICONS = ['checklist', 'file-type-pdf', 'table', 'file-invoice'] as const;

/**
 * Página de um segmento (restaurantes, mercados, lojas, clínicas). Um só modelo,
 * alimentado pelo conteúdo do segmento indicado na rota.
 */
@Component({
  selector: 'app-segment-page',
  imports: [RouterLink, AppIconComponent, PageBreadcrumbComponent, PageClosingComponent],
  templateUrl: './segment-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SegmentPageComponent {
  private readonly routeData = inject(ActivatedRoute).snapshot.data as PublicPageRouteData;
  private readonly slug = this.routeData.slug as SegmentPageSlug;

  protected readonly entry =
    SEGMENT_PAGES.find((page) => page.slug === this.slug) ?? SEGMENT_PAGES[0];
  protected readonly page = SEGMENT_PAGE_CONTENT[this.entry.slug];
  protected readonly example = buildPriceMapExample(this.page.example);
  protected readonly flowIcons = FLOW_ICONS;

  constructor() {
    const seo = inject(SeoService);
    const url = seo.absoluteUrl(`/${this.entry.slug}`);

    const removeStructuredData = seo.setStructuredData('ld-page', {
      '@context': 'https://schema.org',
      '@graph': [
        {
          '@type': 'WebPage',
          name: this.page.headline,
          description: this.entry.description,
          url,
          inLanguage: 'pt-BR',
          about: { ...SOFTWARE_APPLICATION, url: seo.absoluteUrl('/') },
          audience: { '@type': 'BusinessAudience', name: this.entry.label },
        },
        breadcrumbList([
          { name: 'Início', url: seo.absoluteUrl('/') },
          { name: this.entry.label, url },
        ]),
        faqPage(this.page.questions),
      ],
    });
    inject(DestroyRef).onDestroy(removeStructuredData);
  }
}
