import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';

import { SeoService } from '../../../../core/seo/seo.service';

import { AppIconComponent } from '../../../../shared/ui/app-icon/app-icon.component';
import { BrandMarkComponent } from '../../../../shared/ui/brand-mark/brand-mark.component';
import { RevealDirective } from '../../../../shared/ui/reveal/reveal.directive';

interface DemoPrice {
  readonly value: string;
  readonly best: boolean;
}

interface DemoRow {
  readonly item: string;
  readonly quantity: string;
  readonly prices: readonly DemoPrice[];
}

interface Step {
  readonly number: string;
  readonly icon: string;
  readonly title: string;
  readonly text: string;
}

interface Feature {
  readonly icon: string;
  readonly title: string;
  readonly text: string;
}

interface Segment {
  readonly icon: string;
  readonly title: string;
  readonly text: string;
}

interface Question {
  readonly question: string;
  readonly answer: string;
}

const price = (value: string, best = false): DemoPrice => ({ value, best });

/** Dados do mockup: os totais abaixo batem com estas linhas. */
const DEMO_SUPPLIERS = ['Fornecedor A', 'Fornecedor B', 'Fornecedor C'] as const;

const DEMO_ROWS: readonly DemoRow[] = [
  {
    item: 'Material 1',
    quantity: '40 un',
    prices: [price('24,90'), price('23,50', true), price('25,40')],
  },
  {
    item: 'Material 2',
    quantity: '60 un',
    prices: [price('7,20', true), price('7,65'), price('7,45')],
  },
  {
    item: 'Insumo 1',
    quantity: '30 un',
    prices: [price('16,80'), price('17,20'), price('15,90', true)],
  },
  {
    item: 'Insumo 2',
    quantity: '48 un',
    prices: [price('2,35'), price('2,10', true), price('2,40')],
  },
];

const DEMO_TOTALS = ['2.044,80', '2.015,80', '2.055,20'] as const;

type DemoViewId =
  | 'dashboard'
  | 'lists'
  | 'requests'
  | 'price-map'
  | 'equalizations'
  | 'orders'
  | 'suppliers'
  | 'history';

interface DemoNavigationItem {
  readonly id: DemoViewId;
  readonly icon: string;
  readonly label: string;
}

interface DemoView {
  readonly eyebrow: string;
  readonly title: string;
  readonly action: string;
  readonly actionIcon: string;
  readonly kpis: readonly { readonly label: string; readonly value: string }[];
  readonly columns: readonly string[];
  readonly rows: readonly (readonly string[])[];
  /** Quando true, a última coluna é mostrada como selo de situação. */
  readonly statusColumn: boolean;
  /** Quando true, cada linha ganha os atalhos de PDF, Excel e WhatsApp. */
  readonly quickActions?: boolean;
  /** Quando true, a linha também mostra o atalho de leitura por IA. */
  readonly aiAction?: boolean;
  /** Texto do botão azul de IA ao lado da ação principal, quando a tela tem um. */
  readonly aiButton?: string;
  readonly doneStatus?: string;
}

const DEMO_NAVIGATION: readonly DemoNavigationItem[] = [
  { id: 'dashboard', icon: 'dashboard', label: 'Dashboard' },
  { id: 'lists', icon: 'checklist', label: 'Listas de compras' },
  { id: 'requests', icon: 'file-type-pdf', label: 'Solicitações' },
  { id: 'price-map', icon: 'table', label: 'Mapa de preços' },
  { id: 'equalizations', icon: 'scale', label: 'Equalizações' },
  { id: 'orders', icon: 'file-invoice', label: 'Pedidos' },
  { id: 'suppliers', icon: 'buildings', label: 'Fornecedores' },
  { id: 'history', icon: 'history', label: 'Histórico' },
];

/** Telas de demonstração além do mapa de preços, que tem marcação própria no template. */
type DashboardRangeId = 'week' | 'month' | 'year';
type DashboardMetricId = 'orders' | 'lists' | 'requests' | 'equalizations';

interface DashboardRange {
  readonly id: DashboardRangeId;
  readonly label: string;
  /** Como o período é dividido no eixo: "dia", "semana" ou "mês". */
  readonly unit: string;
  readonly axis: readonly string[];
  readonly kpis: readonly { readonly label: string; readonly value: string }[];
}

interface DashboardMetric {
  readonly id: DashboardMetricId;
  readonly label: string;
  readonly singular: string;
  readonly plural: string;
  /** Quantidade em cada ponto do eixo, por período. */
  readonly series: Record<DashboardRangeId, readonly number[]>;
}

const DASHBOARD_RANGES: readonly DashboardRange[] = [
  {
    id: 'week',
    label: '7 dias',
    unit: 'dia',
    axis: ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb', 'Dom'],
    kpis: [
      { label: 'Total comprado', value: 'R$ 2.628,10' },
      { label: 'Pedidos', value: '2' },
      { label: 'Ticket médio', value: 'R$ 1.314,05' },
      { label: 'Fornecedores', value: '1' },
    ],
  },
  {
    id: 'month',
    label: '30 dias',
    unit: 'semana',
    axis: ['Semana 1', 'Semana 2', 'Semana 3', 'Semana 4'],
    kpis: [
      { label: 'Total comprado', value: 'R$ 5.211,50' },
      { label: 'Pedidos', value: '5' },
      { label: 'Ticket médio', value: 'R$ 1.042,30' },
      { label: 'Fornecedores', value: '3' },
    ],
  },
  {
    id: 'year',
    label: '12 meses',
    unit: 'mês',
    axis: ['Dez', 'Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov'],
    kpis: [
      { label: 'Total comprado', value: 'R$ 48.730,00' },
      { label: 'Pedidos', value: '49' },
      { label: 'Ticket médio', value: 'R$ 994,49' },
      { label: 'Fornecedores', value: '5' },
    ],
  },
];

/** O que o gráfico mostra. A série de pedidos soma o indicador "Pedidos" de cada período. */
const DASHBOARD_METRICS: readonly DashboardMetric[] = [
  {
    id: 'orders',
    label: 'Pedidos',
    singular: 'pedido',
    plural: 'pedidos',
    series: {
      week: [0, 1, 0, 1, 0, 0, 0],
      month: [1, 1, 1, 2],
      year: [2, 3, 2, 4, 3, 4, 5, 4, 6, 5, 6, 5],
    },
  },
  {
    id: 'lists',
    label: 'Listas',
    singular: 'lista',
    plural: 'listas',
    series: {
      week: [0, 1, 0, 0, 1, 0, 0],
      month: [1, 1, 2, 1],
      year: [3, 4, 3, 5, 4, 5, 6, 5, 7, 6, 7, 6],
    },
  },
  {
    id: 'requests',
    label: 'Solicitações',
    singular: 'solicitação',
    plural: 'solicitações',
    series: {
      week: [1, 0, 1, 0, 0, 0, 0],
      month: [1, 2, 1, 1],
      year: [3, 3, 4, 4, 5, 5, 6, 5, 7, 6, 7, 6],
    },
  },
  {
    id: 'equalizations',
    label: 'Equalizações',
    singular: 'equalização',
    plural: 'equalizações',
    series: {
      week: [0, 0, 1, 0, 1, 0, 0],
      month: [1, 1, 1, 2],
      year: [2, 3, 3, 4, 3, 5, 5, 4, 6, 5, 6, 6],
    },
  },
];
const DEMO_VIEWS: Record<Exclude<DemoViewId, 'price-map' | 'dashboard'>, DemoView> = {
  history: {
    eyebrow: 'Histórico',
    title: 'Atividade recente',
    action: 'Filtrar',
    actionIcon: 'filter',
    aiButton: 'IA',
    kpis: [
      { label: 'Registros', value: '5' },
      { label: 'Pedidos', value: '1' },
      { label: 'Cotações', value: '2' },
      { label: 'Listas', value: '1' },
    ],
    columns: ['Data', 'Ação', 'Documento', 'Lista'],
    rows: [
      ['27/11', 'Pedido emitido', 'PC-0005', 'Reposição da semana'],
      ['27/11', 'Equalização salva', 'EQ-0005', 'Reposição da semana'],
      ['26/11', 'Cotação respondida', 'SC-0003', 'Reposição da semana'],
      ['25/11', 'Cotação enviada', 'SC-0004', 'Materiais de limpeza'],
      ['24/11', 'Lista criada', 'LC-0005', 'Material de escritório'],
    ],
    statusColumn: false,
  },
  lists: {
    eyebrow: 'Listas de compras',
    title: 'Todas as listas',
    action: 'Nova lista',
    aiButton: 'Criar lista com IA',
    actionIcon: 'plus',
    kpis: [
      { label: 'Listas', value: '5' },
      { label: 'Itens', value: '20' },
      { label: 'Em cotação', value: '2' },
      { label: 'Concluídas', value: '2' },
    ],
    columns: ['Lista', 'Itens', 'Fornecedores', 'Situação'],
    rows: [
      ['Reposição da semana', '4', '3', 'Em cotação'],
      ['Materiais de limpeza', '5', '2', 'Em cotação'],
      ['Reposição do mês', '2', '3', 'Concluída'],
      ['Embalagens', '6', '2', 'Concluída'],
      ['Material de escritório', '3', '2', 'Rascunho'],
    ],
    statusColumn: true,
    doneStatus: 'Concluída',
  },
  requests: {
    eyebrow: 'Solicitações de cotação',
    title: 'Cotações enviadas',
    action: 'Gerar PDF',
    aiButton: 'IA',
    actionIcon: 'file-type-pdf',
    kpis: [
      { label: 'Solicitações', value: '5' },
      { label: 'Itens', value: '20' },
      { label: 'Fornecedores', value: '3' },
      { label: 'Respondidas', value: '3' },
    ],
    columns: ['Código', 'Lista', 'Itens', 'Prazo', 'Situação'],
    rows: [
      ['SC-0005', 'Material de escritório', '3', '02/12', 'Aguardando'],
      ['SC-0004', 'Materiais de limpeza', '5', '30/11', 'Aguardando'],
      ['SC-0003', 'Reposição da semana', '4', '27/11', 'Respondida'],
      ['SC-0002', 'Reposição do mês', '2', '14/11', 'Respondida'],
      ['SC-0001', 'Embalagens', '6', '10/11', 'Respondida'],
    ],
    statusColumn: true,
    doneStatus: 'Respondida',
    quickActions: true,
    aiAction: true,
  },
  equalizations: {
    eyebrow: 'Equalizações',
    title: 'Comparações salvas',
    action: 'Salvar equalização',
    aiButton: 'IA',
    actionIcon: 'scale',
    kpis: [
      { label: 'Salvas', value: '5' },
      { label: 'Cobertura', value: '4 de 4' },
      { label: 'Melhor combinação', value: 'R$ 1.949,80' },
      { label: 'Economia', value: 'R$ 66,00' },
    ],
    columns: ['Código', 'Lista', 'Melhor fornecedor', 'Total'],
    rows: [
      ['EQ-0005', 'Reposição da semana', 'Fornecedor B', 'R$ 2.015,80'],
      ['EQ-0004', 'Reposição do mês', 'Fornecedor A', 'R$ 1.280,00'],
      ['EQ-0003', 'Embalagens', 'Fornecedor C', 'R$ 864,50'],
      ['EQ-0002', 'Materiais de limpeza', 'Fornecedor B', 'R$ 612,30'],
      ['EQ-0001', 'Material de escritório', 'Fornecedor A', 'R$ 438,90'],
    ],
    statusColumn: false,
    quickActions: true,
  },
  orders: {
    eyebrow: 'Pedidos de compra',
    title: 'Pedidos emitidos',
    action: 'Emitir pedido',
    aiButton: 'IA',
    actionIcon: 'file-invoice',
    kpis: [
      { label: 'Pedidos', value: '5' },
      { label: 'Emitidos', value: '2' },
      { label: 'Concluídos', value: '3' },
      { label: 'Total', value: 'R$ 5.211,50' },
    ],
    columns: ['Pedido', 'Fornecedor', 'Total', 'Situação'],
    rows: [
      ['PC-0005', 'Fornecedor B', 'R$ 2.015,80', 'Emitido'],
      ['PC-0004', 'Fornecedor B', 'R$ 612,30', 'Emitido'],
      ['PC-0003', 'Fornecedor A', 'R$ 1.280,00', 'Concluído'],
      ['PC-0002', 'Fornecedor C', 'R$ 864,50', 'Concluído'],
      ['PC-0001', 'Fornecedor A', 'R$ 438,90', 'Concluído'],
    ],
    statusColumn: true,
    doneStatus: 'Concluído',
    quickActions: true,
  },
  suppliers: {
    eyebrow: 'Fornecedores',
    title: 'Fornecedores cadastrados',
    action: 'Novo fornecedor',
    aiButton: 'IA',
    actionIcon: 'plus',
    kpis: [
      { label: 'Fornecedores', value: '5' },
      { label: 'Com cotação', value: '5' },
      { label: 'Itens cotados', value: '17' },
      { label: 'Pedidos', value: '5' },
    ],
    columns: ['Fornecedor', 'Itens cotados', 'Menores preços', 'Pedidos'],
    rows: [
      ['Fornecedor A', '4', '1', '2'],
      ['Fornecedor B', '4', '2', '2'],
      ['Fornecedor C', '4', '1', '1'],
      ['Fornecedor D', '3', '0', '0'],
      ['Fornecedor E', '2', '0', '0'],
    ],
    statusColumn: false,
  },
};

/** Indicadores do mockup, calculados a partir das linhas acima. */
const DEMO_KPIS = [
  { label: 'Fornecedores', value: '3', positive: false },
  { label: 'Itens cotados', value: '4 de 4', positive: false },
  { label: 'Melhor combinação', value: 'R$ 1.949,80', positive: false },
  { label: 'Economia', value: 'R$ 66,00', positive: true },
] as const;

const OLD_WAY: readonly string[] = [
  'Pedido de cotação digitado de novo para cada fornecedor',
  'Preços espalhados em áudios, fotos e conversas',
  'Planilha montada na mão, com fórmula que quebra',
  'Difícil provar por que aquele fornecedor foi escolhido',
];

const NEW_WAY: readonly string[] = [
  'Uma solicitação vira um PDF pronto para enviar',
  'Todos os preços lado a lado, no mesmo mapa',
  'O menor preço de cada item já vem destacado',
  'Histórico salvo de cada comparação e de cada pedido',
];

const STEPS: readonly Step[] = [
  {
    number: '1',
    icon: 'checklist',
    title: 'Monte a solicitação',
    text: 'Liste itens, quantidades e unidades, ou cole da planilha. A IA pode montar a lista para você.',
  },
  {
    number: '2',
    icon: 'file-type-pdf',
    title: 'Envie a cotação em PDF',
    text: 'O PDF sai pronto. Mande por WhatsApp ou e-mail para quantos fornecedores quiser.',
  },
  {
    number: '3',
    icon: 'table',
    title: 'Compare no mapa de preços',
    text: 'Lance os preços ou deixe a IA ler as respostas. Você vê item por item quem cobra menos.',
  },
  {
    number: '4',
    icon: 'file-invoice',
    title: 'Emita o pedido de compra',
    text: 'Escolha o vencedor e gere o pedido em PDF, com tudo registrado.',
  },
];

const FEATURES: readonly Feature[] = [
  {
    icon: 'table',
    title: 'Mapa de preços',
    text: 'Fornecedores lado a lado por item, com o melhor preço em destaque e o total de cada um.',
  },
  {
    icon: 'file-type-pdf',
    title: 'PDF e Excel',
    text: 'Cotação e pedido em PDF padronizado, e o mapa de preços no Excel quando precisar.',
  },
  {
    icon: 'checklist',
    title: 'Listas de compras',
    text: 'Monte a lista digitando ou colando da planilha. Se preferir, a IA organiza os itens para você.',
  },
  {
    icon: 'brand-whatsapp',
    title: 'Envio pelo WhatsApp',
    text: 'Compartilhe a cotação e o pedido no canal que o fornecedor já usa.',
  },
  {
    icon: 'history',
    title: 'Histórico de comparações',
    text: 'Cada equalização fica salva como foi decidida, mesmo que a lista mude depois.',
  },
  {
    icon: 'building-store',
    title: 'Cotações dos fornecedores',
    text: 'Lance os preços recebidos no mapa. Ou envie a resposta do fornecedor e a IA preenche para você revisar.',
  },
  {
    icon: 'dashboard',
    title: 'Dashboard',
    text: 'Acompanhe pedidos, listas, solicitações e equalizações por período, com filtros e exportação.',
  },
  {
    icon: 'buildings',
    title: 'Cadastro de fornecedores',
    text: 'CNPJ, contato e endereço de cada fornecedor, prontos para vincular às suas listas.',
  },
];

const AI_STEPS: readonly Segment[] = [
  {
    icon: 'send',
    title: 'Envie o que o fornecedor mandou',
    text: 'PDF, foto, planilha ou o texto da conversa. Do jeito que chegou.',
  },
  {
    icon: 'sparkles',
    title: 'A IA encontra itens e preços',
    text: 'Ela casa cada preço com o item da sua lista, mesmo com nome ou unidade diferente.',
  },
  {
    icon: 'circle-check',
    title: 'Você revisa e confirma',
    text: 'Nenhum preço entra no mapa sem a sua conferência.',
  },
];

const SEGMENTS: readonly Segment[] = [
  {
    icon: 'shopping-cart',
    title: 'Mercados',
    text: 'Mercearias e mercados de bairro: reposição semanal com muitos itens e vários distribuidores.',
  },
  {
    icon: 'tools-kitchen-2',
    title: 'Restaurantes',
    text: 'Restaurantes, bares e food service: insumos com preço que muda toda semana.',
  },
  {
    icon: 'shopping-bag',
    title: 'Lojas',
    text: 'Comércios em geral: compras recorrentes de mercadoria e material de uso.',
  },
  {
    icon: 'stethoscope',
    title: 'Clínicas',
    text: 'Clínicas e consultórios: materiais e descartáveis com mais de um fornecedor.',
  },
  {
    icon: 'building',
    title: 'E outros negócios',
    text: 'Se a sua empresa cota com mais de um fornecedor, o Planejador de Compras serve para você.',
  },
];

interface Plan {
  readonly name: string;
  readonly price: string;
  readonly period: string;
  readonly items: readonly string[];
  readonly action: string;
  readonly featured: boolean;
}

const PLANS: readonly Plan[] = [
  {
    name: 'Plano Inicial',
    price: 'R$ 0',
    period: 'para começar',
    items: [
      'Listas, fornecedores e cotações',
      'Mapa de preços e equalização',
      'Cotação e pedido de compra em PDF',
      'Exportação do mapa para Excel',
      '20x uso de IA por mês',
    ],
    action: 'Criar minha conta',
    featured: false,
  },
  {
    name: 'Pro',
    price: 'R$ 20',
    period: 'por mês',
    items: [
      'Tudo do Plano Inicial',
      '200x uso de IA por mês',
      'Dashboard com filtros e exportação',
      'Histórico completo de atividades',
    ],
    action: 'Assinar o Pro',
    featured: true,
  },
];

const QUESTIONS: readonly Question[] = [
  {
    question: 'Preciso trocar ou integrar meu ERP?',
    answer:
      'Não. O Planejador de Compras funciona ao lado do que você já usa. Você faz a cotação e a comparação aqui e leva o resultado em PDF ou Excel.',
  },
  {
    question: 'O fornecedor precisa criar conta?',
    answer:
      'Não. Ele recebe o PDF da cotação pelo WhatsApp ou e-mail e responde como já responde hoje. Você lança os preços no mapa.',
  },
  {
    question: 'Funciona no celular?',
    answer: 'Sim. Abre no navegador do celular, do tablet ou do computador, sem instalar nada.',
  },
  {
    question: 'Quanto custa?',
    answer:
      'O Plano Inicial é gratuito e inclui 20x uso de IA por mês. O Pro custa R$ 20 por mês, com 200x uso de IA, dashboard e histórico completos.',
  },
  {
    question: 'Quem vê os meus preços e fornecedores?',
    answer:
      'Só a sua conta. Listas, fornecedores, preços e pedidos de uma conta não aparecem para nenhuma outra.',
  },
  {
    question: 'Posso começar com a planilha que já tenho?',
    answer:
      'Pode. Copie as linhas da planilha e cole na lista: item, quantidade e unidade entram de uma vez.',
  },
];

@Component({
  selector: 'app-landing-page',
  imports: [RouterLink, AppIconComponent, BrandMarkComponent, RevealDirective],
  templateUrl: './landing-page.component.html',
  styleUrl: './landing-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingPageComponent {
  protected readonly demoSuppliers = DEMO_SUPPLIERS;
  protected readonly demoRows = DEMO_ROWS;
  protected readonly demoTotals = DEMO_TOTALS;
  protected readonly demoNavigation = DEMO_NAVIGATION;
  protected readonly activeView = signal<DemoViewId>('price-map');

  constructor() {
    // Mesmos planos e perguntas da página, em formato que buscadores e assistentes de IA leem.
    const removeStructuredData = inject(SeoService).setStructuredData('ld-landing', {
      '@context': 'https://schema.org',
      '@graph': [
        {
          '@type': 'SoftwareApplication',
          name: 'Planejador de Compras',
          applicationCategory: 'BusinessApplication',
          operatingSystem: 'Web',
          inLanguage: 'pt-BR',
          description:
            'Sistema de cotação e pedido de compra para mercados, restaurantes, lojas e clínicas, com ajuda de IA para criar listas e ler as respostas dos fornecedores.',
          featureList: FEATURES.map((feature) => feature.title),
          offers: PLANS.map((plan) => ({
            '@type': 'Offer',
            name: plan.name,
            price: plan.price.replace(/[^0-9]/g, ''),
            priceCurrency: 'BRL',
          })),
        },
        {
          '@type': 'FAQPage',
          mainEntity: QUESTIONS.map((item) => ({
            '@type': 'Question',
            name: item.question,
            acceptedAnswer: { '@type': 'Answer', text: item.answer },
          })),
        },
      ],
    });
    inject(DestroyRef).onDestroy(removeStructuredData);
  }
  /** Tela genérica escolhida no menu; o mapa de preços usa a marcação própria. */
  protected readonly demoView = computed(() => {
    const id = this.activeView();
    return DEMO_VIEWS[id === 'price-map' || id === 'dashboard' ? 'lists' : id];
  });

  protected readonly dashboardRanges = DASHBOARD_RANGES;
  protected readonly dashboardMetrics = DASHBOARD_METRICS;
  protected readonly dashboardRangeId = signal<DashboardRangeId>('month');
  protected readonly dashboardMetricId = signal<DashboardMetricId>('orders');
  protected readonly dashboardRange = computed(
    () => DASHBOARD_RANGES.find((range) => range.id === this.dashboardRangeId()) ?? DASHBOARD_RANGES[1],
  );
  protected readonly dashboardMetric = computed(
    () =>
      DASHBOARD_METRICS.find((metric) => metric.id === this.dashboardMetricId()) ??
      DASHBOARD_METRICS[0],
  );
  /** Geometria do gráfico de linha, em porcentagem da área de desenho. */
  protected readonly dashboardChart = computed(() => {
    const range = this.dashboardRange();
    const metric = this.dashboardMetric();
    const values = metric.series[range.id];
    const yMax = Math.max(2, Math.ceil(Math.max(...values) / 2) * 2);
    const lastIndex = values.length - 1;
    const dots = values.map((value, index) => ({
      label: range.axis[index],
      value,
      x: lastIndex === 0 ? 50 : (index / lastIndex) * 100,
      y: 100 - (value / yMax) * 100,
      tip: `${range.axis[index]}: ${value} ${value === 1 ? metric.singular : metric.plural}`,
    }));
    const line = dots.map((dot, index) => `${index === 0 ? 'M' : 'L'}${dot.x} ${dot.y}`).join(' ');

    return {
      title: `${metric.label} por ${range.unit}`,
      dots,
      line,
      area: `${line} L100 100 L0 100 Z`,
      ticks: [yMax, yMax / 2, 0],
    };
  });
  protected readonly demoKpis = DEMO_KPIS;
  protected readonly oldWay = OLD_WAY;
  protected readonly newWay = NEW_WAY;
  protected readonly steps = STEPS;
  protected readonly features = FEATURES;
  protected readonly segments = SEGMENTS;
  protected readonly aiSteps = AI_STEPS;
  protected readonly questions = QUESTIONS;
  protected readonly plans = PLANS;

  protected selectView(view: DemoViewId): void {
    this.activeView.set(view);
  }

  protected selectDashboardRange(range: DashboardRangeId): void {
    this.dashboardRangeId.set(range);
  }

  protected selectDashboardMetric(metric: DashboardMetricId): void {
    this.dashboardMetricId.set(metric);
  }
}
