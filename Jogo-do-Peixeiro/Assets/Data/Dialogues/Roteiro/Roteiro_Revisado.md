# Roteiro revisado - diálogos e cutscenes

Este arquivo é uma referência rápida para revisar o texto fora do Inspector. Os assets editáveis ficam nas subpastas `Cutscenes`, `Regular` e `EdgeCases`.

As partes marcadas como `[CENA/CUTSCENE OPCIONAL]` são instruções de cena, câmera ou encenação. Se não houver cutscene implementada e o diálogo for usado apenas como texto padrão, essas partes podem ser removidas. As falas dos personagens devem ser mantidas.

## Cena de introdução - parte 1

[CENA/CUTSCENE OPCIONAL]
Tela escura. Lentamente, a tela clareia. Marina está em frente à sua loja, olhando para ela.

Marina: Finalmente arrumei minhas coisas. Cheguei na ilha mó cansada, mas tudo valeu a pena. A loja tá prontinha, melhor do que eu imaginava também! E mal tive que gastar pra fazer ela, hehe.

Marina: Opa, verdade. O Cobrador me chamou para a cabana dele. Melhor ir lá checar.

Fim da parte 1;

## Cena de introdução - parte 2

[CENA/CUTSCENE OPCIONAL]
O player interage com a cabana do Cobrador. Marina está em frente à cabana.

Marina: OPA, COBRADOR, TU TÁ AÍ, MANO??

...

Marina: Às vezes ele saiu pra comer algo. Vou ver se-

Cobrador: Opa, opa. Perdão pela demora, eu estava... ocupado.

Marina: Ah, tá. Com o quê? Fazendo suas coisas misteriosas de cobrador?

Cobrador: De certa forma, mas isso não importa.

Cobrador: Mas então, o que achou da loja? Fiz questão de pegar apenas a madeira de maior qualidade para o acabamento.

Marina: Ficou supimpa, hein. Poderia morar lá e ficaria de boa.

Cobrador: Mas você não mora lá? Pediu pra eu colocar uma cama e tudo.

Marina: Claro, uai. Onde mais eu moraria?

Cobrador: ... Quer saber? Eu não vou perguntar.

Cobrador: De qualquer maneira, ainda temos um último assunto a conversar antes de nosso contrato acabar, que obviamente é-

Marina: Um valeu e um obrigado? Poxa, muito obrigada, né? Hahaha.

Cobrador: ... O pagamento, Marina. Estou falando do quanto você me deve.

Marina: Oh... esqueci dessa parte, hehe...

Cobrador: Bem, a quantia que você me deve é mais ou menos 1100 moedas.

Marina: 1100?? EU MAL FAÇO 100 EM UM MÊS!

Cobrador: É sério? Que pena. Esse foi o custo. Então, ou você paga, ou diz adeus para sua lojinha preciosa.

Marina: M-mas... eu não tenho como pagar...

Cobrador: Olha, a gente pode fazer um trato. Você vai me pagando em partes e eu não pego a sua loja, mas cada entrega tem prazo de 3 dias. Me ouviu?

Marina: C-claro! Mas eu não sei como eu conseguiria o dinheiro. A venda dos peixes só começa no fim do mês, eu pesco mais agora pra guardar...

[CENA/CUTSCENE OPCIONAL]
A câmera vira para a direção da doca.

Cobrador: Tá vendo a doca lá? Ouvi dizer que o dono daquele lugar compra peixes. Você é uma pescadora, não é? Então começa a pescar logo.

Marina: O-ok! Obrigada!

Fim da parte 2;

## Cena do porto

O player interage com o Dono do Porto.

Marina: Opa, bom dia, senhor. Tudo bem?

Dono do Porto: Uma cara nova nessa ilhazinha? Raridade hoje em dia. Do que precisa, amiga?

Marina: B-bem, eu acabei me metendo numa enrascada e...

Dono do Porto: Você pegou dinheiro do Cobrador e agora tem que pescar peixes para conseguir dinheiro e pagar a dívida antes que ele pegue de volta tudo que ele fez, te deixe sem um estabelecimento e sem um local para morar, te forçando a morar no mato da ilha.

Dono do Porto: Correto?

Marina: Hã... foi bem isso sim.

Dono do Porto: Acredite, você não é a primeira a abrir uma loja de pesca nessa ilha.

Marina: Que... específico.

Dono do Porto: Bem, já que você está aqui, quer dizer que ele te disse que eu compraria seus peixes. E sim, eu compro.

Marina: Sério??? Caramba, muito obrigada!!

Dono do Porto: Não tem de quê, amiga. Se precisar de iscas ou de alguém que melhore seus equipamentos, pode falar comigo. Ajudo no que puder.

Dono do Porto: Agora pegue seu barco e comece a pescar, se quiser manter sua loja, claro.

Marina: Barco? Mas eu não tenho um barco.

Dono do Porto: Ué, então como você chegou aqui?

Marina: Táxi.

Dono do Porto: ...

Dono do Porto: Eu te empresto meu barco.

Fim;

## Cena do vulcão

O player interage com o Dono do Porto dentro do vulcão.

Dono do Porto: Marina! Você chegou bem longe para entrar aqui, hein.

Marina: Seu dono! Que bom te ver aqui. Vai ficar mais fácil vender os peixes, hehe.

Marina: ... Pera, mas se eu peguei seu barco, como que você chegou aqui?

Dono do Porto: Andando.

Marina: ... Ok??

Fim;

## Cena final da campanha

[CENA/CUTSCENE OPCIONAL]
Logo depois de o jogador entregar a última entrega, Marina está em frente à sua loja.

Marina: Aahh... finalmente. Agora posso viver uma vida boa em uma ilha pacífica, sem mais nada para me parar.

Marina: Tô cansada pra caramba. Vou tirar um cochilo!

[IMPLEMENTAÇÃO SEM CUTSCENE]
Se não houver cutscene implementada, fazer apenas um fade out e fade in, mudando para a manhã seguinte. Depois disso, continuar o diálogo a partir da cabana do Cobrador.

IMPORTANTE: como esta sequência acontece no final da campanha, a passagem de dia não deve contar como falha de prazo, game over, entrega perdida ou penalidade do sistema de dia/noite.

Marina: Por que você me chamou aqui de novo, Cobrador...? Eu já paguei a loja, não paguei?

Cobrador: De fato, você pagou a loja, Marina. Mas acho que se esqueceu dos Air Fishers que comprou com meu dinheiro...

Marina: Oooh, aqueles tênis superlegais, né? Eles são mó estilosos, não são? Hehe.

Cobrador: Tão estilosos que você se... recusa a usar eles?

Marina: Claro, uai. Não quero estragar eles!

Cobrador: Bem, isso não importa muito. Você, por acaso, viu o preço deles?

Marina: Claro, 100 moedas. Isso daí eu já tenho guardado.

Cobrador: ... Eram 100 MILHÕES de moedas, Marina.

Marina: ...............

Cobrador: Eu recomendo que você volte a pescar, a não ser que você não queira mais esses tênis.

Marina: EU PAGO! EU PAGO! TUDO MENOS ISSO, POR FAVOR!

Fim;

## Script tutorial

Diálogo introdução parte 1;

Slides mostrando os controles de movimento e interação;

Diálogo introdução parte 2;

Slide mostrando o Dono do Porto;

Diálogo do porto;

Slide mostrando controle de pesca e do barco;

Slide explicando que precisa entregar o dinheiro e sobre o limite de horas acordada;

Fim do tutorial.

## Edge cases

### Dono do Porto antes da introdução parte 2

Marina: ...

Dono do Porto: ...

Marina: ...

Dono do Porto: ............

Dono do Porto: Posso te ajudar...?

Marina: Ah, não. Eu só gosto de observar pessoas.

Dono do Porto: .... Ok?

Fim;

### Barco antes da rota principal

Marina: Oooh, é parecido com o barco que me trouxe aqui!

Fim;

## Extras - diálogos com o Cobrador

### Fala 1

Cobrador: Já voltou? Foi bem rápida, hein. Agora, onde está meu dinheiro?

Fim;

### Fala 2

Cobrador: Então, Marina, como vai a pesca?

Marina: Ah! Tá indo-

Cobrador: Eu não me importo. Agora cadê o meu dinheiro?

Fim;

### Fala 3

Marina: Sabe, às vezes acho que você teria mais amigos se não ficasse obrigando pessoas a trabalhar pra te dar dinheiro.

Cobrador: Caramba, eu nunca pensei nisso. Realmente muda muito meu ponto de vista da vida.

Marina: Sério? Quer dizer que eu não preciso mais pagar??

Cobrador: Claro que precisa. Eu só quis dizer que eu não preciso de amigos com todo o dinheiro que eu tenho. Agora paga logo, vai.

Fim;

### Fala 4

Cobrador: Os peixes dessa ilha são tão estranhos... mas o mercado por eles é surpreendentemente grande.

Marina: Por que será?

Cobrador: Gente rica compra animais pra mostruário. Se eu me importasse com o que eles fazem, eu não teria dinheiro. Agora, você ia me pagar, não ia?

Fim;

### Fala 5

Cobrador: Demorou. Tava pensando que você tentou fugir sem pagar, hahaha. Péssima escolha.

Cobrador: De qualquer maneira, pode ir pagando.

Fim;

### Fala 6

Cobrador: Está com o dinheiro?

Marina: De certa maneira, sim.

Cobrador: Você não pode só dizer "de certa maneira". Me passa logo o dinheiro!

Fim;

### Fala 7

Marina: Me diz, já que você nunca sai dessa cabana, o que você come?

Cobrador: Dinheiro e as almas dos inocentes. Agora me paga logo antes que eu pegue a sua.

Fim;
