import { SegmentPageSlug } from '../public-pages';
import { SegmentPageContent } from '../models/public-page.model';

/**
 * Texto das páginas por segmento. Cada uma fala da rotina daquele público com exemplos
 * próprios; os preços do mapa de exemplo são ilustrativos.
 */
export const SEGMENT_PAGE_CONTENT: Record<SegmentPageSlug, SegmentPageContent> = {
  'para-restaurantes': {
    slug: 'para-restaurantes',
    eyebrow: 'Para restaurantes, bares e cozinhas',
    headline: 'Cotação de insumos para restaurantes, sem caderno e sem áudio perdido',
    lead: 'Para restaurantes, bares, lanchonetes e cozinhas que compram insumos toda semana, o Planejador de Compras organiza a lista da cozinha, envia a cotação em PDF aos fornecedores pelo WhatsApp e mostra, item por item, quem cobrou menos antes de você fechar o pedido.',
    painsTitle: 'O que complica a compra da cozinha',
    pains: [
      {
        title: 'Preço que muda de uma semana para outra',
        text: 'Tomate, cebola, carne e laticínios variam a cada compra. Lembrar quanto custou na semana passada não basta para saber se a proposta de hoje é boa.',
      },
      {
        title: 'Cada fornecedor responde de um jeito',
        text: 'O hortifrúti manda áudio, o açougue manda foto da tabela, a distribuidora manda PDF. Juntar tudo isso toma o tempo de quem deveria estar cuidando da produção.',
      },
      {
        title: 'Unidade diferente em cada proposta',
        text: 'Um cota por quilo, outro por caixa, outro por maço. Sem colocar os preços na mesma unidade, o mais barato pode ser só o que parece mais barato.',
      },
    ],
    exampleTitle: 'Como fica a compra da semana no mapa de preços',
    exampleText:
      'Três fornecedores responderam à mesma lista. O menor preço de cada insumo aparece em destaque, e o total de cada um fica embaixo para comparar a compra inteira.',
    example: {
      listName: 'Hortifrúti, proteínas e laticínios da semana',
      suppliers: ['Hortifrúti', 'Distribuidora', 'Atacado'],
      rows: [
        { item: 'Tomate italiano', quantity: 25, unit: 'kg', prices: [7.9, 8.4, 7.6] },
        { item: 'Cebola', quantity: 20, unit: 'kg', prices: [4.5, 4.2, 4.8] },
        { item: 'Filé de peito de frango', quantity: 30, unit: 'kg', prices: [19.9, 18.7, 19.2] },
        { item: 'Muçarela fatiada', quantity: 8, unit: 'kg', prices: [41.5, 39.9, 42.3] },
        {
          item: 'Creme de leite, caixa 1 L',
          quantity: 12,
          unit: 'un',
          prices: [18.2, 17.95, 17.8],
        },
      ],
    },
    flowTitle: 'Da lista da cozinha ao pedido',
    flow: [
      {
        title: 'Monte a lista da semana',
        text: 'Lance os insumos com quantidade e unidade (kg, caixa, maço) ou cole as linhas da planilha que a cozinha já usa.',
      },
      {
        title: 'Mande a cotação pelo WhatsApp',
        text: 'O sistema gera um PDF com os itens. Você encaminha para o hortifrúti, o açougue e a distribuidora, sem redigitar a lista para cada um.',
      },
      {
        title: 'Compare no mapa de preços',
        text: 'Lance os preços recebidos. O menor de cada insumo fica em destaque e o total de cada fornecedor aparece ao lado.',
      },
      {
        title: 'Emita o pedido de compra',
        text: 'Feche com um ou mais fornecedores e gere o pedido em PDF, que serve também para conferir a mercadoria na entrega.',
      },
    ],
    questions: [
      {
        question: 'Dá para comprar cada insumo de um fornecedor diferente?',
        answer:
          'Dá. A equalização mostra a melhor combinação item a item, e você decide se divide a compra ou concentra em um fornecedor só.',
      },
      {
        question: 'O hortifrúti precisa usar o sistema para responder?',
        answer:
          'Não. Ele recebe o PDF da cotação pelo WhatsApp e responde como já responde hoje. Quem lança os preços no mapa é você.',
      },
      {
        question: 'A IA ajuda em quê na rotina da cozinha?',
        answer:
          'É uma ajuda opcional: ela pode montar a lista a partir de um texto e ler a resposta do fornecedor para preencher o mapa. Nenhum preço entra sem a sua conferência.',
      },
    ],
    closingTitle: 'Organize a próxima compra da cozinha',
    closingText:
      'Crie a conta, cadastre os fornecedores que você já usa e mande a primeira cotação ainda esta semana. É gratuito e de código aberto.',
  },

  'para-mercados': {
    slug: 'para-mercados',
    eyebrow: 'Para mercados, mercearias e empórios',
    headline: 'Cotação para mercados: a reposição inteira comparada em um só mapa',
    lead: 'Para mercados de bairro, mercearias, minimercados e empórios, o Planejador de Compras transforma a lista de reposição em uma cotação em PDF, reúne os preços de vários distribuidores e mostra o menor preço de cada item antes de você emitir o pedido.',
    painsTitle: 'Por que a reposição dá tanto trabalho',
    pains: [
      {
        title: 'Lista longa para muitos distribuidores',
        text: 'Mercearia, bebidas, limpeza e higiene somam dezenas de itens por reposição. Digitar a mesma lista para cada distribuidor é trabalho repetido toda semana.',
      },
      {
        title: 'Embalagem e marca que não batem',
        text: 'Um distribuidor cota o fardo com 12, outro a caixa com 6; um oferece outra marca. Para comparar, todos precisam responder sobre o mesmo item, na mesma unidade.',
      },
      {
        title: 'Diferença que sai da margem',
        text: 'Quando o preço de compra não é comparado item a item, a diferença entre um distribuidor e outro some sem ninguém perceber, direto da margem de venda.',
      },
    ],
    exampleTitle: 'Uma reposição de mercearia no mapa de preços',
    exampleText:
      'Mesma lista, três distribuidores. Cada um ganha em itens diferentes, e o total mostra qual seria a compra inteira com cada fornecedor.',
    example: {
      listName: 'Reposição de mercearia, bebidas e limpeza',
      suppliers: ['Distribuidora A', 'Distribuidora B', 'Atacarejo'],
      rows: [
        { item: 'Arroz tipo 1, pacote 5 kg', quantity: 20, unit: 'un', prices: [26.9, 25.8, 26.4] },
        { item: 'Feijão carioca, pacote 1 kg', quantity: 30, unit: 'un', prices: [7.4, 7.65, 7.2] },
        { item: 'Óleo de soja 900 ml', quantity: 24, unit: 'un', prices: [7.95, 7.7, 7.85] },
        { item: 'Refrigerante 2 L', quantity: 36, unit: 'un', prices: [8.1, 8.35, 7.9] },
        { item: 'Detergente 500 ml', quantity: 48, unit: 'un', prices: [2.3, 2.15, 2.4] },
      ],
    },
    flowTitle: 'Da lista de reposição ao pedido',
    flow: [
      {
        title: 'Cole a lista de reposição',
        text: 'Copie as linhas da sua planilha de reposição e cole: item, quantidade e unidade entram de uma vez.',
      },
      {
        title: 'Envie a mesma cotação a todos',
        text: 'Um único PDF serve para todos os distribuidores, com os mesmos itens na mesma ordem. Isso deixa a comparação justa depois.',
      },
      {
        title: 'Veja quem ganha item por item',
        text: 'No mapa de preços, cada linha mostra o distribuidor mais barato. O total de cada coluna mostra quem ganha na compra inteira.',
      },
      {
        title: 'Feche o pedido com quem venceu',
        text: 'Gere o pedido de compra em PDF e, se precisar guardar ou mostrar a decisão, exporte o mapa para Excel.',
      },
    ],
    questions: [
      {
        question: 'Como comparar distribuidores que cotam embalagens diferentes?',
        answer:
          'Defina na lista a unidade de compra de cada item (fardo, caixa ou unidade) e peça a cotação nessa unidade. Assim os preços lançados no mapa ficam na mesma base.',
      },
      {
        question: 'Preciso integrar com o sistema do caixa?',
        answer:
          'Não. O Planejador de Compras cuida da cotação e do pedido e funciona ao lado do sistema que o mercado já usa, sem integração.',
      },
      {
        question: 'Fica registrado qual distribuidor ganhou cada reposição?',
        answer:
          'Fica. Cada equalização salva guarda a comparação como foi decidida, mesmo que a lista seja alterada depois.',
      },
    ],
    closingTitle: 'Compare a próxima reposição',
    closingText:
      'Crie a conta, cole a lista da semana e envie a cotação aos distribuidores. O sistema é gratuito e tem o código aberto.',
  },

  'para-lojas': {
    slug: 'para-lojas',
    eyebrow: 'Para lojas e comércio em geral',
    headline: 'Cotação de compras para lojas, da mercadoria ao material de uso',
    lead: 'Para lojas que compram com frequência, como papelarias, pet shops, lojas de utilidades e de material de construção, o Planejador de Compras reúne a lista, a cotação em PDF, a comparação entre fornecedores e o pedido de compra em um lugar só.',
    painsTitle: 'Onde a compra da loja se perde',
    pains: [
      {
        title: 'Revenda e uso interno misturados',
        text: 'Mercadoria da vitrine, sacolas, etiquetas e bobinas vêm de fornecedores diferentes e acabam na mesma planilha, sem separação.',
      },
      {
        title: 'Proposta em todo canal',
        text: 'Uma chega pelo representante, outra por catálogo, outra por mensagem. Sem um lugar único para comparar, a melhor oferta fica esquecida em alguma conversa.',
      },
      {
        title: 'Decisão difícil de explicar',
        text: 'Quando o sócio pergunta por que a compra foi feita com aquele fornecedor, a resposta está espalhada entre e-mails e prints.',
      },
    ],
    exampleTitle: 'Uma compra de reposição da loja no mapa de preços',
    exampleText:
      'Itens de revenda e de uso interno na mesma cotação. O destaque mostra o menor preço de cada linha; o total, o custo da compra com cada fornecedor.',
    example: {
      listName: 'Reposição de papelaria e material de uso',
      suppliers: ['Atacadista', 'Representante', 'Distribuidor'],
      rows: [
        {
          item: 'Caderno universitário 10 matérias',
          quantity: 40,
          unit: 'un',
          prices: [18.9, 17.6, 18.2],
        },
        {
          item: 'Caneta esferográfica azul, caixa com 50',
          quantity: 6,
          unit: 'cx',
          prices: [42.0, 44.5, 39.9],
        },
        {
          item: 'Bobina para impressora térmica',
          quantity: 30,
          unit: 'un',
          prices: [3.2, 3.45, 3.1],
        },
        {
          item: 'Etiqueta adesiva de preço, rolo',
          quantity: 10,
          unit: 'un',
          prices: [6.5, 5.9, 6.2],
        },
        {
          item: 'Sacola plástica, pacote com 100',
          quantity: 15,
          unit: 'pct',
          prices: [12.8, 13.4, 12.5],
        },
      ],
    },
    flowTitle: 'Do pedido do balcão ao pedido de compra',
    flow: [
      {
        title: 'Separe o que vai cotar',
        text: 'Monte uma lista para mercadoria de revenda e outra para material de uso, cada uma com os fornecedores certos.',
      },
      {
        title: 'Envie a solicitação em PDF',
        text: 'O fornecedor recebe um documento padronizado com os itens, por WhatsApp ou e-mail, e responde como preferir.',
      },
      {
        title: 'Compare com a mesma régua',
        text: 'As respostas viram colunas no mapa de preços. O menor preço de cada item aparece em destaque, junto do total por fornecedor.',
      },
      {
        title: 'Registre o pedido',
        text: 'O pedido de compra sai em PDF e fica guardado com a comparação que levou a ele.',
      },
    ],
    questions: [
      {
        question: 'Vale a pena para uma loja com poucos fornecedores?',
        answer:
          'Vale. Com dois fornecedores já faz sentido comparar no mapa de preços, e o cadastro guarda CNPJ, contato e endereço de cada um para as próximas compras.',
      },
      {
        question: 'Consigo levar a comparação para fora do sistema?',
        answer: 'Consegue. O mapa de preços pode ser exportado em Excel e em PDF.',
      },
      {
        question: 'Tem algum custo?',
        answer: 'Não. O Planejador de Compras é gratuito e o código é aberto.',
      },
    ],
    closingTitle: 'Tire a compra da loja da planilha',
    closingText:
      'Crie a conta, cadastre os fornecedores e monte a primeira lista. Você pode começar colando a planilha que já tem.',
  },

  'para-clinicas': {
    slug: 'para-clinicas',
    eyebrow: 'Para clínicas e consultórios',
    headline: 'Cotação de materiais para clínicas, com registro de cada compra',
    lead: 'Para clínicas médicas, odontológicas, veterinárias, de estética e consultórios, o Planejador de Compras organiza a cotação de descartáveis e materiais de consumo, compara os distribuidores item por item e gera o pedido de compra em PDF, com o histórico salvo.',
    painsTitle: 'O que pesa na compra da clínica',
    pains: [
      {
        title: 'Material que não pode faltar',
        text: 'Luvas, máscaras e gaze acabam rápido. Quando a cotação fica parada esperando alguém juntar as respostas, quem sente é o atendimento.',
      },
      {
        title: 'Mesmo item, especificação diferente',
        text: 'Tamanho da luva, caixa com 50 ou com 100, estéril ou não: a comparação só vale quando todos cotam exatamente o mesmo material.',
      },
      {
        title: 'Quem decidiu e por quê',
        text: 'Com sócios ou gestor, a compra precisa de registro: o que foi cotado, quem respondeu e qual proposta foi escolhida.',
      },
    ],
    exampleTitle: 'A reposição de descartáveis no mapa de preços',
    exampleText:
      'Três distribuidores cotaram a mesma lista, com a mesma especificação. O destaque aponta o menor preço de cada material.',
    example: {
      listName: 'Reposição mensal de descartáveis',
      suppliers: ['Distribuidor A', 'Distribuidor B', 'Distribuidor C'],
      rows: [
        {
          item: 'Luva de procedimento M, caixa com 100',
          quantity: 20,
          unit: 'cx',
          prices: [32.9, 34.5, 31.8],
        },
        {
          item: 'Máscara tripla, caixa com 50',
          quantity: 15,
          unit: 'cx',
          prices: [18.5, 17.9, 19.2],
        },
        {
          item: 'Gaze estéril, pacote com 10',
          quantity: 50,
          unit: 'pct',
          prices: [3.4, 3.25, 3.6],
        },
        { item: 'Álcool 70%, 1 L', quantity: 24, unit: 'un', prices: [9.9, 10.4, 9.6] },
        {
          item: 'Lençol descartável, rolo 70 cm x 50 m',
          quantity: 12,
          unit: 'rl',
          prices: [24.0, 22.8, 23.5],
        },
      ],
    },
    flowTitle: 'Da reposição ao pedido, com registro',
    flow: [
      {
        title: 'Liste com a especificação',
        text: 'Descreva tamanho, embalagem e quantidade de cada material para que todos os distribuidores cotem a mesma coisa.',
      },
      {
        title: 'Peça a cotação aos distribuidores',
        text: 'Envie o PDF da solicitação pelo WhatsApp ou por e-mail. O distribuidor não precisa criar conta.',
      },
      {
        title: 'Compare e salve a equalização',
        text: 'O mapa de preços mostra o menor preço de cada item. A equalização salva não muda depois, mesmo que a lista seja editada.',
      },
      {
        title: 'Emita o pedido em PDF',
        text: 'O pedido de compra registra fornecedor, itens e valores, e fica no histórico da clínica.',
      },
    ],
    questions: [
      {
        question: 'O sistema controla estoque ou validade dos materiais?',
        answer:
          'Não. O Planejador de Compras cuida da cotação e do pedido de compra. Estoque e validade continuam no sistema que a clínica já usa.',
      },
      {
        question: 'Quem vê os preços e fornecedores da clínica?',
        answer:
          'Só a sua conta. Listas, fornecedores, preços e pedidos não aparecem para nenhuma outra conta.',
      },
      {
        question: 'A IA escolhe o fornecedor?',
        answer:
          'Não. A IA é uma ajuda opcional para montar a lista e ler respostas de fornecedores. A escolha é sua, e todo preço passa pela sua revisão.',
      },
    ],
    closingTitle: 'Deixe a próxima reposição registrada',
    closingText:
      'Crie a conta, cadastre os distribuidores da clínica e envie a primeira cotação. Gratuito e de código aberto.',
  },
};
