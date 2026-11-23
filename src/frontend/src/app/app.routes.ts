import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { PUBLIC_PAGE_ROUTES } from './features/public-pages/public-pages.routes';
import { LoginPageComponent } from './features/auth/pages/login-page/login-page.component';
import { AppLayoutComponent } from './layout/app-layout/app-layout.component';
import { PublicLayoutComponent } from './layout/public-layout/public-layout.component';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Entrar | Planejador de Compras',
    component: LoginPageComponent,
    data: { authMode: 'login', noindex: true },
  },
  {
    path: 'cadastro',
    title: 'Criar conta | Planejador de Compras',
    component: LoginPageComponent,
    data: { authMode: 'register', noindex: true },
  },
  {
    path: 'esqueci-minha-senha',
    title: 'Recuperar senha | Planejador de Compras',
    component: LoginPageComponent,
    data: { authMode: 'forgot-password', noindex: true },
  },
  {
    path: 'reset-password',
    title: 'Redefinir senha | Planejador de Compras',
    component: LoginPageComponent,
    data: { authMode: 'reset-password', noindex: true },
  },
  {
    path: 'confirmar-email',
    title: 'Confirmar e-mail | Planejador de Compras',
    component: LoginPageComponent,
    data: { authMode: 'confirm-email', noindex: true },
  },
  {
    // Página pública do fornecedor: o token do link é a credencial, sem login.
    path: 'cotacao/:token',
    title: 'Responder cotação | Planejador de Compras',
    data: { noindex: true },
    loadComponent: () =>
      import(
        './features/supplier-portal/pages/supplier-quotation-page/supplier-quotation-page.component'
      ).then(({ SupplierQuotationPageComponent }) => SupplierQuotationPageComponent),
  },
  {
    path: '',
    component: PublicLayoutComponent,
    children: [
      {
        path: '',
        pathMatch: 'full',
        title: 'Planejador de Compras | Cotação de compras com IA',
        data: {
          description:
            'Planeje suas compras com IA: monte a lista, envie a cotação em PDF pelo WhatsApp, compare fornecedores no mapa de preços e emita o pedido. Para mercados, restaurantes, lojas e clínicas.',
        },
        loadComponent: () =>
          import('./features/landing/pages/landing-page/landing-page.component').then(
            ({ LandingPageComponent }) => LandingPageComponent,
          ),
      },
      {
        path: 'politica-de-privacidade',
        title: 'Política de Privacidade | Planejador de Compras',
        data: {
          legalDocument: 'privacy',
          description: 'Como o Planejador de Compras coleta, usa e protege os seus dados.',
        },
        loadComponent: () =>
          import('./features/legal/pages/legal-page/legal-page.component').then(
            ({ LegalPageComponent }) => LegalPageComponent,
          ),
      },
      {
        path: 'termos-de-uso',
        title: 'Termos de Uso | Planejador de Compras',
        data: {
          legalDocument: 'terms',
          description: 'Condições de uso do Planejador de Compras.',
        },
        loadComponent: () =>
          import('./features/legal/pages/legal-page/legal-page.component').then(
            ({ LegalPageComponent }) => LegalPageComponent,
          ),
      },
      {
        path: 'politica-de-cookies',
        title: 'Política de Cookies | Planejador de Compras',
        data: {
          legalDocument: 'cookies',
          description: 'Quais cookies o Planejador de Compras usa e para quê.',
        },
        loadComponent: () =>
          import('./features/legal/pages/legal-page/legal-page.component').then(
            ({ LegalPageComponent }) => LegalPageComponent,
          ),
      },
      ...PUBLIC_PAGE_ROUTES,
    ],
  },
  {
    path: 'app',
    component: AppLayoutComponent,
    data: { noindex: true },
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        title: 'Listas de compras | Planejador de Compras',
        loadComponent: () =>
          import('./features/shopping-lists/pages/shopping-lists-page/shopping-lists-page.component').then(
            ({ ShoppingListsPageComponent }) => ShoppingListsPageComponent,
          ),
      },
      {
        path: 'price-map/:listId/items/new',
        redirectTo: 'price-map/:listId',
        pathMatch: 'full',
      },
      {
        path: 'price-map/:listId',
        loadComponent: () =>
          import('./features/price-map/pages/price-map-page/price-map-page.component').then(
            ({ PriceMapPageComponent }) => PriceMapPageComponent,
          ),
      },
      {
        path: 'price-map',
        title: 'Mapa de preços | Planejador de Compras',
        loadComponent: () =>
          import('./features/price-map/pages/price-map-page/price-map-page.component').then(
            ({ PriceMapPageComponent }) => PriceMapPageComponent,
          ),
      },
      { path: 'items/new/:listId', redirectTo: 'price-map/:listId', pathMatch: 'full' },
      { path: 'items', redirectTo: 'price-map', pathMatch: 'full' },
      { path: 'quotes', redirectTo: 'price-map', pathMatch: 'full' },
      {
        path: 'quotation-requests/:id',
        title: 'Solicitação de cotação | Planejador de Compras',
        loadComponent: () =>
          import('./features/quotation-requests/pages/quotation-request-detail-page/quotation-request-detail-page.component').then(
            ({ QuotationRequestDetailPageComponent }) => QuotationRequestDetailPageComponent,
          ),
      },
      {
        path: 'quotation-requests',
        title: 'Solicitações de cotação | Planejador de Compras',
        loadComponent: () =>
          import('./features/quotation-requests/pages/quotation-requests-page/quotation-requests-page.component').then(
            ({ QuotationRequestsPageComponent }) => QuotationRequestsPageComponent,
          ),
      },
      {
        path: 'suppliers',
        title: 'Fornecedores | Planejador de Compras',
        loadComponent: () =>
          import('./features/suppliers/pages/suppliers-page/suppliers-page.component').then(
            ({ SuppliersPageComponent }) => SuppliersPageComponent,
          ),
      },
      {
        path: 'purchase-orders/new',
        title: 'Emitir pedido de compra | Planejador de Compras',
        loadComponent: () =>
          import('./features/purchase-orders/pages/purchase-order-form-page/purchase-order-form-page.component').then(
            ({ PurchaseOrderFormPageComponent }) => PurchaseOrderFormPageComponent,
          ),
      },
      {
        path: 'purchase-orders/:id',
        title: 'Pedido de compra | Planejador de Compras',
        loadComponent: () =>
          import('./features/purchase-orders/pages/purchase-order-detail-page/purchase-order-detail-page.component').then(
            ({ PurchaseOrderDetailPageComponent }) => PurchaseOrderDetailPageComponent,
          ),
      },
      {
        path: 'purchase-orders',
        title: 'Pedidos de compra | Planejador de Compras',
        loadComponent: () =>
          import('./features/purchase-orders/pages/purchase-orders-page/purchase-orders-page.component').then(
            ({ PurchaseOrdersPageComponent }) => PurchaseOrdersPageComponent,
          ),
      },
      {
        path: 'equalizations/:id',
        title: 'Equalização salva | Planejador de Compras',
        loadComponent: () =>
          import('./features/equalization-history/pages/saved-equalization-detail-page/saved-equalization-detail-page.component').then(
            ({ SavedEqualizationDetailPageComponent }) => SavedEqualizationDetailPageComponent,
          ),
      },
      {
        path: 'equalizations',
        title: 'Histórico de equalizações | Planejador de Compras',
        loadComponent: () =>
          import('./features/equalization-history/pages/equalization-history-page/equalization-history-page.component').then(
            ({ EqualizationHistoryPageComponent }) => EqualizationHistoryPageComponent,
          ),
      },
      { path: 'equalization', redirectTo: 'equalizations', pathMatch: 'full' },
      {
        path: 'lists/:listId/equalization',
        loadComponent: () =>
          import('./features/equalization/pages/equalization-page/equalization-page.component').then(
            ({ EqualizationPageComponent }) => EqualizationPageComponent,
          ),
      },
      {
        path: 'lists/:listId/items/:itemId/quotes',
        redirectTo: 'price-map/:listId',
        pathMatch: 'full',
      },
      {
        path: 'lists/:listId',
        redirectTo: 'price-map/:listId',
        pathMatch: 'full',
      },
    ],
  },
  {
    path: '**',
    component: PublicLayoutComponent,
    children: [
      {
        path: '',
        title: 'Página não encontrada | Planejador de Compras',
        data: { noindex: true },
        loadComponent: () =>
          import('./features/landing/pages/not-found-page/not-found-page.component').then(
            ({ NotFoundPageComponent }) => NotFoundPageComponent,
          ),
      },
    ],
  },
];
