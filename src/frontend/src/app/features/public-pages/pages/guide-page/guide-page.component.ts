import { ChangeDetectionStrategy, Component, DestroyRef, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { SeoService } from '../../../../core/seo/seo.service';
import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { PageBreadcrumbComponent } from '../../components/page-breadcrumb/page-breadcrumb.component';
import { PageClosingComponent } from '../../components/page-closing/page-closing.component';
import { GUIDE_PAGE_CONTENT } from '../../content/guide-pages.content';
import { SOFTWARE_APPLICATION, breadcrumbList, faqPage } from '../../models/structured-data';
import { GUIDE_PAGES, GuidePageSlug, PublicPageRouteData } from '../../public-pages';

/** Guia de compras (mapa de cotação, equalização, planilha). Um só modelo para todos. */
@Component({
  selector: 'app-guide-page',
  imports: [AppIconComponent, PageBreadcrumbComponent, PageClosingComponent],
  templateUrl: './guide-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GuidePageComponent {
  private readonly routeData = inject(ActivatedRoute).snapshot.data as PublicPageRouteData;
  private readonly slug = this.routeData.slug as GuidePageSlug;

  protected readonly entry = GUIDE_PAGES.find((page) => page.slug === this.slug) ?? GUIDE_PAGES[0];
  protected readonly page = GUIDE_PAGE_CONTENT[this.entry.slug];
  protected readonly updatedAtLabel = new Intl.DateTimeFormat('pt-BR', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(`${this.page.updatedAt}T00:00:00Z`));

  constructor() {
    const seo = inject(SeoService);
    const url = seo.absoluteUrl(`/${this.entry.slug}`);
    const publisher = {
      '@type': 'Organization',
      name: 'Planejador de Compras',
      url: seo.absoluteUrl('/'),
    };

    const removeStructuredData = seo.setStructuredData('ld-page', {
      '@context': 'https://schema.org',
      '@graph': [
        {
          '@type': 'Article',
          headline: this.page.headline,
          description: this.entry.description,
          url,
          mainEntityOfPage: url,
          inLanguage: 'pt-BR',
          dateModified: this.page.updatedAt,
          author: publisher,
          publisher,
          about: { ...SOFTWARE_APPLICATION, url: seo.absoluteUrl('/') },
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
