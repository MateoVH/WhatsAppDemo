'use strict';

// Bandeja en tiempo real: carga el estado por REST y lo mantiene al día con los eventos de SignalR.
// La interfaz está en español e inglés. El servidor envía códigos (estados, notas, fallas) y aquí se traducen,
// así cada persona ve la bandeja en su idioma aunque varias la miren a la vez.

const I18N = {
  es: {
    locale: 'es-CO',
    title: 'Bandeja de WhatsApp con IA',
    'brand.mark': 'Bandeja',
    'brand.tag': 'WhatsApp + IA',
    'chips.label': 'Estado de las integraciones',
    'chip.aiDemo': 'IA simulada (sin API key)',
    'chip.twilioOn': 'Twilio WhatsApp',
    'chip.twilioOff': 'Twilio sin configurar',
    'connection.connecting': 'Conectando…',
    'connection.live': 'En vivo',
    'connection.reconnecting': 'Reconectando…',
    'connection.offline': 'Sin conexión',
    'language.label': 'Idioma',
    'sidebar.title': 'Conversaciones',
    'sidebar.simulate': 'Simular mensaje',
    'sidebar.empty': 'Aún no hay conversaciones.',
    'onboarding.eyebrow': 'Sandbox de WhatsApp de Twilio',
    'onboarding.title': 'Escribe por WhatsApp y mira llegar el mensaje',
    'onboarding.step1': 'Desde tu teléfono, envía {code} al {number}. El código está en la consola de Twilio.',
    'onboarding.joinCode': 'join <tu-código>',
    'onboarding.step2': 'Escribe una pregunta, por ejemplo «¿A qué hora abren hoy?».',
    'onboarding.step3': 'El mensaje aparece aquí y la IA responde en segundos. Puedes tomar la conversación cuando quieras.',
    'onboarding.alt': '¿Sin teléfono a mano?',
    'onboarding.simulate': 'Simula un mensaje',
    placeholder: 'Elige una conversación para ver los mensajes.',
    'chat.back': 'Volver a las conversaciones',
    'chat.simulated': 'simulada',
    'handoff.label': 'Responde',
    'author.ai': 'IA',
    'author.agent': 'Asesor',
    'owner.ai': 'Responde la IA',
    'owner.agent': 'Responde un asesor',
    'messages.label': 'Mensajes',
    'composer.label': 'Respuesta',
    'composer.placeholderAi': 'Escribe para tomar la conversación…',
    'composer.placeholderAgent': 'Escribe tu respuesta…',
    'composer.hintAi': 'La IA está respondiendo. Si envías un mensaje, se pausa y respondes tú.',
    'composer.hintAgent': 'Enter envía · Shift+Enter, nueva línea',
    'composer.send': 'Enviar',
    'composer.sendError': 'No se pudo enviar: {error}',
    'composer.ownerError': 'No se pudo cambiar quién responde: {error}',
    typing: 'IA escribiendo…',
    'typing.aria': 'La IA está escribiendo',
    'status.pending': 'Enviando…',
    'status.sent': 'Enviado por WhatsApp',
    'status.simulated': 'Simulado: no salió por WhatsApp',
    'status.failed': 'No se envió',
    'message.failed': 'No se envió: {error}',
    'message.unknownError': 'error desconocido',
    unread: '{count} sin leer',
    'tag.simulated': 'Conversación simulada',
    'note.autoReplyOn': '🤖 IA reactivada: responderá los próximos mensajes',
    'note.autoReplyOff': '🙋 IA en pausa: un asesor atiende la conversación',
    'note.aiFailed': '⚠️ La IA no pudo responder: {reason}',
    'failure.invalidApiKey': 'la API key de Anthropic no es válida.',
    'failure.modelNotFound': 'el modelo configurado no existe o tu cuenta no tiene acceso.',
    'failure.rateLimited': 'se alcanzó el límite de uso de la API. Prueba de nuevo en unos segundos.',
    'failure.unavailable': 'la API de Anthropic no está disponible en este momento.',
    'failure.network': 'no hay conexión con la API de Anthropic.',
    'failure.apiError': 'la API de Anthropic devolvió un error (detalle en los logs).',
    'failure.refused': 'el modelo declinó responder este mensaje. Revisa la conversación.',
    'failure.emptyReply': 'el modelo no devolvió texto.',
    'failure.unexpected': 'error inesperado (detalle en los logs).',
    'simulator.title': 'Simular mensaje de cliente',
    'simulator.lead': 'Llega a la bandeja como si viniera de WhatsApp. Las respuestas no salen a ningún teléfono.',
    'simulator.name': 'Nombre',
    'simulator.phone': 'Teléfono',
    'simulator.message': 'Mensaje',
    'simulator.ideas': 'Ideas de mensaje',
    'simulator.cancel': 'Cancelar',
    'simulator.submit': 'Enviar mensaje',
    'simulator.error': 'No se pudo simular el mensaje: {error}',
    'simulator.ideaList': ['¿A qué hora abren hoy?', '¿Hacen domicilios a Laureles?', 'Quiero 2 capuchinos y un croissant', 'Quiero hablar con una persona'],
    'simulator.names': ['Valentina Ríos', 'Santiago Gómez', 'Camila Restrepo', 'Andrés Torres', 'Laura Cardona', 'Felipe Osorio'],
  },
  en: {
    locale: 'en-US',
    title: 'WhatsApp AI Inbox',
    'brand.mark': 'Inbox',
    'brand.tag': 'WhatsApp + AI',
    'chips.label': 'Integration status',
    'chip.aiDemo': 'Simulated AI (no API key)',
    'chip.twilioOn': 'Twilio WhatsApp',
    'chip.twilioOff': 'Twilio not configured',
    'connection.connecting': 'Connecting…',
    'connection.live': 'Live',
    'connection.reconnecting': 'Reconnecting…',
    'connection.offline': 'Offline',
    'language.label': 'Language',
    'sidebar.title': 'Conversations',
    'sidebar.simulate': 'Simulate message',
    'sidebar.empty': 'No conversations yet.',
    'onboarding.eyebrow': 'Twilio WhatsApp Sandbox',
    'onboarding.title': 'Send a WhatsApp message and watch it arrive',
    'onboarding.step1': 'From your phone, send {code} to {number}. You’ll find the code in the Twilio Console.',
    'onboarding.joinCode': 'join <your-code>',
    'onboarding.step2': 'Ask a question, for example “What time do you open today?”',
    'onboarding.step3': 'The message shows up here and the AI replies within seconds. You can take over the conversation at any time.',
    'onboarding.alt': 'No phone at hand?',
    'onboarding.simulate': 'Simulate a message',
    placeholder: 'Pick a conversation to see its messages.',
    'chat.back': 'Back to conversations',
    'chat.simulated': 'simulated',
    'handoff.label': 'Replying',
    'author.ai': 'AI',
    'author.agent': 'Agent',
    'owner.ai': 'The AI is replying',
    'owner.agent': 'An agent is replying',
    'messages.label': 'Messages',
    'composer.label': 'Reply',
    'composer.placeholderAi': 'Type to take over the conversation…',
    'composer.placeholderAgent': 'Type your reply…',
    'composer.hintAi': 'The AI is replying. If you send a message, it pauses and you take over.',
    'composer.hintAgent': 'Enter to send · Shift+Enter for a new line',
    'composer.send': 'Send',
    'composer.sendError': 'Couldn’t send: {error}',
    'composer.ownerError': 'Couldn’t change who replies: {error}',
    typing: 'AI is typing…',
    'typing.aria': 'The AI is typing',
    'status.pending': 'Sending…',
    'status.sent': 'Sent via WhatsApp',
    'status.simulated': 'Simulated: not sent via WhatsApp',
    'status.failed': 'Not sent',
    'message.failed': 'Not sent: {error}',
    'message.unknownError': 'unknown error',
    unread: '{count} unread',
    'tag.simulated': 'Simulated conversation',
    'note.autoReplyOn': '🤖 AI back on: it will answer the next messages',
    'note.autoReplyOff': '🙋 AI paused: an agent is handling the conversation',
    'note.aiFailed': '⚠️ The AI couldn’t reply: {reason}',
    'failure.invalidApiKey': 'the Anthropic API key is invalid.',
    'failure.modelNotFound': 'the configured model doesn’t exist or your account can’t access it.',
    'failure.rateLimited': 'the API rate limit was reached. Try again in a few seconds.',
    'failure.unavailable': 'the Anthropic API is unavailable right now.',
    'failure.network': 'can’t reach the Anthropic API.',
    'failure.apiError': 'the Anthropic API returned an error (details in the logs).',
    'failure.refused': 'the model declined to answer this message. Please review the conversation.',
    'failure.emptyReply': 'the model returned no text.',
    'failure.unexpected': 'unexpected error (details in the logs).',
    'simulator.title': 'Simulate a customer message',
    'simulator.lead': 'It reaches the inbox as if it came from WhatsApp. Replies are never sent to a phone.',
    'simulator.name': 'Name',
    'simulator.phone': 'Phone',
    'simulator.message': 'Message',
    'simulator.ideas': 'Message ideas',
    'simulator.cancel': 'Cancel',
    'simulator.submit': 'Send message',
    'simulator.error': 'Couldn’t simulate the message: {error}',
    'simulator.ideaList': ['What time do you open today?', 'Do you deliver to Laureles?', 'I’d like 2 cappuccinos and a croissant', 'I want to talk to a person'],
    'simulator.names': ['Emily Carter', 'James Walker', 'Olivia Bennett', 'Noah Mitchell', 'Ava Thompson', 'Liam Brooks'],
  },
};

const state = {
  lang: 'es',
  loaded: false,
  status: null,
  connection: 'connecting',
  conversations: new Map(),
  selectedId: null,
  messages: [],
  typing: new Set(),
  unread: new Map(),
  fresh: new Set(),
};

const byId = (id) => document.getElementById(id);
const els = {
  app: document.querySelector('.app'),
  chipAi: byId('chip-ai'),
  chipWhatsApp: byId('chip-whatsapp'),
  chipLive: byId('chip-live'),
  list: byId('conversations'),
  listEmpty: byId('list-empty'),
  onboarding: byId('onboarding'),
  stepJoin: byId('step-join'),
  placeholder: byId('placeholder'),
  chat: byId('chat'),
  chatAvatar: byId('chat-avatar'),
  chatName: byId('chat-name'),
  chatPhone: byId('chat-phone'),
  handoff: byId('handoff'),
  messages: byId('messages'),
  composer: byId('composer'),
  composerText: byId('composer-text'),
  composerHint: byId('composer-hint'),
  send: byId('send'),
  back: byId('back'),
  simulator: byId('simulator'),
  simulatorForm: byId('simulator-form'),
  simulatorError: byId('simulator-error'),
  ideas: byId('ideas'),
};

const wideLayout = window.matchMedia('(min-width: 761px)');

const ICONS = {
  ai: '<svg class="icon icon--fill" viewBox="0 0 24 24" aria-hidden="true"><path d="M12 2.5l2.1 6.4 6.4 2.1-6.4 2.1L12 19.5l-2.1-6.4L3.5 11l6.4-2.1z"/></svg>',
  agent: '<svg class="icon" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="8" r="3.5"/><path d="M5 20c1.2-3.6 4-5.5 7-5.5s5.8 1.9 7 5.5"/></svg>',
  pending: '<svg class="icon" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="8"/><path d="M12 8v4l2.5 2"/></svg>',
  sent: '<svg class="icon" viewBox="0 0 24 24" aria-hidden="true"><path d="M5 12.5l4.5 4.5L19 7.5"/></svg>',
  failed: '<svg class="icon" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9"/><path d="M12 7.5v5.5M12 16.5v.01"/></svg>',
};

const AUTHORS = {
  ai: { label: 'author.ai', icon: ICONS.ai },
  agent: { label: 'author.agent', icon: ICONS.agent },
};

const STATUSES = {
  pending: { icon: ICONS.pending, title: 'status.pending' },
  sent: { icon: ICONS.sent, title: 'status.sent' },
  simulated: { text: 'sim', title: 'status.simulated' },
  failed: { icon: ICONS.failed, title: 'status.failed' },
};

const CONNECTION = {
  connecting: ['connection.connecting', 'warn'],
  live: ['connection.live', 'live'],
  reconnecting: ['connection.reconnecting', 'warn'],
  offline: ['connection.offline', 'error'],
};

// ---------- Idioma ----------

let formats = makeFormats('es');

function makeFormats(lang) {
  const { locale } = I18N[lang];
  return {
    time: new Intl.DateTimeFormat(locale, { hour: 'numeric', minute: '2-digit' }),
    day: new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short' }),
  };
}

/** Traduce una clave; {nombre} se reemplaza con params.nombre (si no viene, queda para fill()). */
function t(key, params = {}) {
  const value = I18N[state.lang][key] ?? I18N.es[key] ?? key;
  return typeof value === 'string'
    ? value.replace(/\{(\w+)\}/g, (placeholder, name) => (name in params ? params[name] : placeholder))
    : value;
}

function initialLanguage() {
  try {
    const saved = localStorage.getItem('inbox.lang');
    if (saved && saved in I18N) return saved;
  } catch {
    // Almacenamiento bloqueado: se usa el idioma del navegador.
  }
  return (navigator.languages?.[0] ?? navigator.language ?? 'es').toLowerCase().startsWith('es') ? 'es' : 'en';
}

function setLanguage(lang, { remember = false } = {}) {
  state.lang = lang;
  formats = makeFormats(lang);
  document.documentElement.lang = lang;
  if (remember) {
    try {
      localStorage.setItem('inbox.lang', lang);
    } catch {
      // Sin almacenamiento la elección dura hasta recargar.
    }
  }

  for (const el of document.querySelectorAll('[data-i18n]')) el.textContent = t(el.dataset.i18n);
  for (const el of document.querySelectorAll('[data-i18n-attr]')) {
    for (const pair of el.dataset.i18nAttr.split(';')) {
      const [attribute, key] = pair.split(':');
      el.setAttribute(attribute, t(key));
    }
  }
  for (const button of document.querySelectorAll('[data-lang]')) {
    button.setAttribute('aria-pressed', String(button.dataset.lang === lang));
  }

  fill(els.stepJoin, t('onboarding.step1'), { code: h('code', {}, t('onboarding.joinCode')), number: sandboxNumber });
  els.ideas.replaceChildren(...t('simulator.ideaList').map((idea) => h('button', { class: 'idea', type: 'button' }, idea)));
  renderStatusChips();
  render();
}

// ---------- Utilidades ----------

/** Crea un elemento. Los hijos de texto se insertan como texto, nunca como HTML. */
function h(tag, props = {}, ...children) {
  const el = document.createElement(tag);
  for (const [key, value] of Object.entries(props)) {
    if (value == null || value === false) continue;
    if (key === 'class') el.className = value;
    else if (key === 'html') el.innerHTML = value; // solo para íconos propios o texto ya escapado
    else if (key.startsWith('on')) el.addEventListener(key.slice(2), value);
    else el.setAttribute(key, value === true ? '' : value);
  }
  el.append(...children.flat().filter((child) => child != null && child !== false));
  return el;
}

/** Rellena una plantilla traducida, p. ej. "envía {code} al {number}", con nodos en lugar de HTML. */
function fill(el, template, parts) {
  el.replaceChildren(...template.split(/(\{\w+\})/).filter(Boolean).map((piece) => {
    const name = /^\{(\w+)\}$/.exec(piece)?.[1];
    return name && parts[name] ? parts[name] : piece;
  }));
}

const escapeHtml = (text) =>
  text.replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);

/** Formato de WhatsApp (*negrita*, _cursiva_, ~tachado~, ```código```) sobre texto ya escapado. */
function formatWhatsApp(text) {
  const inline = (marker) =>
    new RegExp(`(^|[\\s(¡¿])\\${marker}(?=\\S)([^\\${marker}\\n]*?\\S)\\${marker}(?=$|[\\s.,;:!?)])`, 'g');
  return escapeHtml(text)
    .replace(/```([\s\S]+?)```/g, '<code>$1</code>')
    .replace(inline('*'), '$1<strong>$2</strong>')
    .replace(inline('_'), '$1<em>$2</em>')
    .replace(inline('~'), '$1<s>$2</s>');
}

function formatPhone(phone) {
  const match = /^\+(57|1)(\d{3})(\d{3})(\d{4})$/.exec(phone.replace(/[^\d+]/g, ''));
  return match ? `+${match[1]} ${match[2]} ${match[3]} ${match[4]}` : phone;
}

function initials(conversation) {
  const words = conversation.name.split(/\s+/).filter((word) => /\p{L}/u.test(word));
  return words.length > 0
    ? (words[0][0] + (words[1]?.[0] ?? '')).toUpperCase()
    : conversation.phone.slice(-2);
}

function toneFor(id) {
  let hash = 0;
  for (const char of id) hash = (hash * 31 + char.charCodeAt(0)) >>> 0;
  return hash % 6;
}

function shortTime(iso) {
  const date = new Date(iso);
  return date.toDateString() === new Date().toDateString() ? formats.time.format(date) : formats.day.format(date);
}

const clockTime = (iso) => formats.time.format(new Date(iso));

const pick = (items) => items[Math.floor(Math.random() * items.length)];

const randomDigits = (count) => String(Math.floor(Math.random() * 10 ** count)).padStart(count, '0');

async function request(method, path, body) {
  const response = await fetch(path, {
    method,
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!response.ok) throw new Error(await describeError(response));
  return response.status === 200 ? response.json() : null;
}

async function describeError(response) {
  try {
    const problem = await response.json();
    return (problem.errors ? Object.values(problem.errors).flat().join(' ') : problem.title) || `Error ${response.status}`;
  } catch {
    return `Error ${response.status}`;
  }
}

// ---------- Render ----------

const sandboxNumber = h('strong', {}, '+1 415 523 8886');

function render() {
  renderConversations();
  renderMain();
}

function setChip(chip, text, tone) {
  chip.dataset.tone = tone;
  chip.title = text;
  chip.querySelector('.chip__text').textContent = text;
}

function renderStatusChips() {
  const { status } = state;
  if (status) {
    setChip(els.chipAi, status.ai.isLive ? status.ai.name : t('chip.aiDemo'), status.ai.isLive ? 'ai' : 'off');
    setChip(els.chipWhatsApp, t(status.whatsApp.isLive ? 'chip.twilioOn' : 'chip.twilioOff'), status.whatsApp.isLive ? 'live' : 'off');
    sandboxNumber.textContent = formatPhone(status.whatsApp.number);
  }
  const [key, tone] = CONNECTION[state.connection];
  setChip(els.chipLive, t(key), tone);
}

function renderConversations() {
  const items = [...state.conversations.values()]
    .sort((a, b) => Date.parse(b.updatedAt) - Date.parse(a.updatedAt))
    .map(renderConversation);
  els.list.replaceChildren(...items);
  els.listEmpty.hidden = items.length > 0;

  const unread = [...state.unread.values()].reduce((sum, count) => sum + count, 0);
  document.title = `${unread ? `(${unread}) ` : ''}${t('title')}`;
}

function renderConversation(conversation) {
  const unread = state.unread.get(conversation.id) ?? 0;
  return h('li', {},
    h('button', {
      class: 'conversation',
      type: 'button',
      'aria-current': conversation.id === state.selectedId ? 'true' : null,
      onclick: () => openConversation(conversation.id),
    },
      renderAvatar(conversation, h('span', { class: 'avatar' })),
      h('span', { class: 'conversation__body' },
        h('span', { class: 'conversation__top' },
          h('span', { class: 'conversation__name' }, conversation.name),
          conversation.isSimulated ? h('span', { class: 'tag', title: t('tag.simulated') }, 'SIM') : null,
          h('time', { class: 'conversation__time', datetime: conversation.updatedAt }, shortTime(conversation.updatedAt))),
        h('span', { class: 'conversation__bottom' },
          renderPreview(conversation),
          unread ? h('span', { class: 'badge', 'aria-label': t('unread', { count: unread }) }, String(unread)) : null))));
}

function renderPreview(conversation) {
  if (state.typing.has(conversation.id)) {
    return h('span', { class: 'conversation__preview preview--typing' }, t('typing'));
  }
  const last = conversation.lastMessage;
  const author = last && AUTHORS[last.author];
  return h('span', { class: 'conversation__preview' },
    author ? h('b', { class: `preview--${last.author}` }, `${t(author.label)}: `) : null,
    last?.text ?? '');
}

function renderAvatar(conversation, el) {
  el.textContent = initials(conversation);
  el.style.setProperty('--tone', `var(--tone-${toneFor(conversation.id)})`);
  el.dataset.owner = conversation.autoReply ? 'ai' : 'agent';
  el.title = t(conversation.autoReply ? 'owner.ai' : 'owner.agent');
  return el;
}

function renderMain() {
  syncPanels();
  const selected = state.conversations.get(state.selectedId);
  if (selected) {
    renderChatHeader(selected);
    renderMessages(true);
  }
}

function syncPanels() {
  const hasSelection = state.conversations.has(state.selectedId);
  els.onboarding.hidden = !state.loaded || state.conversations.size > 0;
  els.placeholder.hidden = state.conversations.size === 0 || hasSelection;
  els.chat.hidden = !hasSelection;
  els.app.dataset.view = hasSelection ? 'thread' : 'list';
}

function renderChatHeader(conversation) {
  renderAvatar(conversation, els.chatAvatar);
  els.chatName.textContent = conversation.name;
  els.chatPhone.textContent = formatPhone(conversation.phone) + (conversation.isSimulated ? ` · ${t('chat.simulated')}` : '');
  for (const option of els.handoff.querySelectorAll('[data-owner]')) {
    option.setAttribute('aria-pressed', String((option.dataset.owner === 'ai') === conversation.autoReply));
  }
  els.composerText.placeholder = t(conversation.autoReply ? 'composer.placeholderAi' : 'composer.placeholderAgent');
  if (!('error' in els.composerHint.dataset)) {
    els.composerHint.textContent = t(conversation.autoReply ? 'composer.hintAi' : 'composer.hintAgent');
  }
}

// La conversación abierta se dibuja completa al abrirla; después los eventos solo
// agregan o retocan elementos sueltos, para no cortar las animaciones de entrada.
let typingEl = null;

function renderMessages(forceScroll = false) {
  const stick = forceScroll || isNearBottom();
  els.messages.replaceChildren(...state.messages.map((message, index) => renderItem(message, authorBefore(index))));
  typingEl = null;
  syncTyping();
  state.fresh.clear();
  if (stick) scrollToBottom();
}

function appendMessage(message, index) {
  const stick = message.author !== 'customer' || isNearBottom();
  els.messages.insertBefore(renderItem(message, authorBefore(index)), typingEl);
  state.fresh.delete(message.id);
  if (stick) scrollToBottom();
}

function syncTyping() {
  typingEl?.remove();
  typingEl = null;
  if (!state.typing.has(state.selectedId)) return;

  const stick = isNearBottom();
  typingEl = renderTyping(authorBefore(state.messages.length));
  els.messages.append(typingEl);
  if (stick) scrollToBottom();
}

/** Autor del mensaje anterior, para agrupar burbujas seguidas del mismo autor. */
function authorBefore(index) {
  const previous = state.messages[index - 1];
  return previous && previous.author !== 'system' ? previous.author : null;
}

function isNearBottom() {
  const list = els.messages;
  return list.scrollHeight - list.scrollTop - list.clientHeight < 120;
}

function scrollToBottom() {
  els.messages.scrollTop = els.messages.scrollHeight;
}

function renderItem(message, previousAuthor) {
  const el = message.author === 'system' ? renderNote(message) : renderMessage(message, previousAuthor);
  el.dataset.id = message.id;
  return el;
}

function messageClass(message, first) {
  return ['msg', `msg--${message.author}`, first && 'msg--first', state.fresh.has(message.id) && 'msg--new']
    .filter(Boolean).join(' ');
}

function renderWho(author) {
  return h('span', { class: 'msg__who', html: AUTHORS[author].icon }, t(AUTHORS[author].label));
}

function renderMessage(message, previous) {
  const first = message.author !== previous;
  return h('li', { class: messageClass(message, first) },
    first && AUTHORS[message.author] ? renderWho(message.author) : null,
    h('div', { class: 'bubble' },
      h('div', { class: 'bubble__text', html: formatWhatsApp(message.text) }),
      h('div', { class: 'bubble__meta' },
        h('time', { datetime: message.createdAt }, clockTime(message.createdAt)),
        message.author === 'customer' ? null : renderStatus(message))),
    message.status === 'failed' ? renderError(message) : null);
}

function renderError(message) {
  return h('p', { class: 'msg__error' }, t('message.failed', { error: message.error ?? t('message.unknownError') }));
}

function renderStatus(message) {
  const status = STATUSES[message.status];
  if (!status) return null;
  return h('span', {
    class: `status status--${message.status}`,
    role: 'img',
    title: t(status.title),
    'aria-label': t(status.title),
    html: status.icon,
  }, status.text);
}

function noteText(message) {
  if (message.note === 'aiFailed') {
    const reason = `failure.${message.error}`;
    return t('note.aiFailed', { reason: t(reason in I18N[state.lang] ? reason : 'failure.unexpected') });
  }
  return message.note ? t(`note.${message.note}`) : message.text;
}

function renderNote(message) {
  return h('li', { class: `note${state.fresh.has(message.id) ? ' msg--new' : ''}` },
    h('span', { class: 'note__text' }, noteText(message), h('time', { datetime: message.createdAt }, clockTime(message.createdAt))));
}

function renderTyping(previous) {
  const first = previous !== 'ai';
  return h('li', { class: `msg msg--ai msg--typing${first ? ' msg--first' : ''}` },
    first ? renderWho('ai') : null,
    h('div', { class: 'bubble', role: 'status', 'aria-label': t('typing.aria') },
      h('span', { class: 'dots', 'aria-hidden': 'true' }, h('i'), h('i'), h('i'))));
}

function showHint(text) {
  els.composerHint.textContent = text;
  els.composerHint.dataset.error = '';
}

function clearHintError() {
  if (!('error' in els.composerHint.dataset)) return;
  delete els.composerHint.dataset.error;
  const selected = state.conversations.get(state.selectedId);
  if (selected) renderChatHeader(selected);
}

function autosize() {
  const input = els.composerText;
  input.style.height = 'auto';
  input.style.height = `${Math.min(input.scrollHeight, 160)}px`;
}

// ---------- Acciones ----------

async function openConversation(id, { quiet = false } = {}) {
  if (id !== state.selectedId) state.messages = [];
  state.selectedId = id;
  state.unread.delete(id);
  history.replaceState(null, '', `#${encodeURIComponent(id)}`);

  try {
    const detail = await request('GET', `/api/conversations/${encodeURIComponent(id)}`);
    if (state.selectedId !== id) return;
    state.conversations.set(id, detail.conversation);
    state.messages = detail.messages;
  } catch {
    closeConversation();
    return;
  }

  render();
  if (!quiet && wideLayout.matches) els.composerText.focus();
}

function closeConversation() {
  state.selectedId = null;
  state.messages = [];
  history.replaceState(null, '', location.pathname);
  render();
}

/** Inserta o reemplaza el mensaje (ordenado por seq) y devuelve si era nuevo. */
function upsertMessage(message) {
  const index = state.messages.findIndex((m) => m.id === message.id);
  if (index >= 0) {
    state.messages[index] = message;
    return false;
  }
  state.messages.push(message);
  state.messages.sort((a, b) => a.seq - b.seq);
  return true;
}

async function sendReply(event) {
  event.preventDefault();
  const id = state.selectedId;
  const text = els.composerText.value.trim();
  if (!id || !text) return;

  els.send.disabled = true;
  try {
    await request('POST', `/api/conversations/${encodeURIComponent(id)}/messages`, { text });
    els.composerText.value = '';
    autosize();
  } catch (error) {
    showHint(t('composer.sendError', { error: error.message }));
  } finally {
    els.send.disabled = false;
    els.composerText.focus();
  }
}

async function changeOwner(event) {
  const option = event.target.closest('[data-owner]');
  const id = state.selectedId;
  if (!option || !id || option.getAttribute('aria-pressed') === 'true') return;

  try {
    const conversation = await request('PUT', `/api/conversations/${encodeURIComponent(id)}/auto-reply`, {
      enabled: option.dataset.owner === 'ai',
    });
    onConversationUpdated(conversation);
  } catch (error) {
    showHint(t('composer.ownerError', { error: error.message }));
  }
}

function openSimulator() {
  const fields = els.simulatorForm.elements;
  const current = state.conversations.get(state.selectedId);
  if (current?.isSimulated) {
    fields.namedItem('customer').value = current.name;
    fields.namedItem('phone').value = formatPhone(current.phone);
  } else {
    fields.namedItem('customer').value = pick(t('simulator.names'));
    fields.namedItem('phone').value = state.lang === 'es' ? `+57 300 555 ${randomDigits(4)}` : `+1 202 555 01${randomDigits(2)}`;
  }
  fields.namedItem('text').value = '';
  els.simulatorError.hidden = true;
  els.simulator.showModal();
  fields.namedItem('text').focus();
}

async function simulateMessage(event) {
  event.preventDefault();
  const fields = els.simulatorForm.elements;
  const phone = fields.namedItem('phone').value.trim();
  const text = fields.namedItem('text').value.trim();
  if (!text) return;

  try {
    await request('POST', '/api/simulator/messages', { name: fields.namedItem('customer').value.trim(), phone, text });
    els.simulator.close();
    await openConversation(`sim-${phone.replace(/\D/g, '')}`);
  } catch (error) {
    els.simulatorError.textContent = t('simulator.error', { error: error.message });
    els.simulatorError.hidden = false;
  }
}

// ---------- Eventos en tiempo real ----------

function onMessageAdded(message) {
  if (message.conversationId === state.selectedId) {
    state.fresh.add(message.id);
    const isNew = upsertMessage(message);
    const index = state.messages.indexOf(message);
    if (isNew && index === state.messages.length - 1) appendMessage(message, index);
    else renderMessages();
    return;
  }

  if (message.author === 'customer') {
    state.unread.set(message.conversationId, (state.unread.get(message.conversationId) ?? 0) + 1);
    // Sin conversación abierta (en pantallas anchas), se abre sola la que acaba de escribir.
    if (!state.selectedId && wideLayout.matches) openConversation(message.conversationId, { quiet: true });
  }
}

function onMessageUpdated(message) {
  if (message.conversationId !== state.selectedId) return;
  upsertMessage(message);

  // Solo cambia el estado de entrega: se retoca la burbuja existente.
  const el = els.messages.querySelector(`[data-id="${CSS.escape(message.id)}"]`);
  if (!el) {
    renderMessages();
    return;
  }
  const status = renderStatus(message);
  const current = el.querySelector('.status');
  if (current && status) current.replaceWith(status);
  else if (status) el.querySelector('.bubble__meta').append(status);
  else current?.remove();
  el.querySelector('.msg__error')?.remove();
  if (message.status === 'failed') el.append(renderError(message));
}

function onConversationUpdated(conversation) {
  state.conversations.set(conversation.id, conversation);
  renderConversations();
  syncPanels();
  if (conversation.id === state.selectedId) renderChatHeader(conversation);
}

function onTypingChanged(conversationId, isTyping) {
  if (isTyping) state.typing.add(conversationId);
  else state.typing.delete(conversationId);
  renderConversations();
  if (conversationId === state.selectedId) syncTyping();
}

// ---------- Conexión ----------

function setConnection(key) {
  state.connection = key;
  renderStatusChips();
}

async function reload() {
  try {
    const [status, conversations] = await Promise.all([request('GET', '/api/status'), request('GET', '/api/conversations')]);
    state.status = status;
    renderStatusChips();

    state.loaded = true;
    state.conversations = new Map(conversations.map((c) => [c.id, c]));
    if (state.conversations.has(state.selectedId)) {
      await openConversation(state.selectedId, { quiet: true });
    } else {
      closeConversation();
    }
  } catch {
    // El servidor no responde: se vuelve a intentar al reconectar.
  }
}

const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/inbox')
  .withAutomaticReconnect()
  .configureLogging(signalR.LogLevel.Warning)
  .build();

connection.on('MessageAdded', onMessageAdded);
connection.on('MessageUpdated', onMessageUpdated);
connection.on('ConversationUpdated', onConversationUpdated);
connection.on('TypingChanged', onTypingChanged);
connection.onreconnecting(() => setConnection('reconnecting'));
connection.onreconnected(() => {
  setConnection('live');
  reload();
});
connection.onclose(() => {
  setConnection('offline');
  setTimeout(connect, 5000);
});

async function connect() {
  try {
    await connection.start();
    setConnection('live');
  } catch {
    setConnection('offline');
    setTimeout(connect, 5000);
  }
  await reload();
}

// ---------- Inicio ----------

document.addEventListener('click', (event) => {
  const action = event.target.closest('[data-action]')?.dataset.action;
  if (action === 'simulate') openSimulator();
  if (action === 'close-simulator') els.simulator.close();

  const lang = event.target.closest('[data-lang]')?.dataset.lang;
  if (lang && lang !== state.lang) setLanguage(lang, { remember: true });

  const idea = event.target.closest('.idea');
  if (idea) {
    const text = els.simulatorForm.elements.namedItem('text');
    text.value = idea.textContent;
    text.focus();
  }
});

els.handoff.addEventListener('click', changeOwner);
els.composer.addEventListener('submit', sendReply);
els.composerText.addEventListener('input', () => {
  autosize();
  clearHintError();
});
els.composerText.addEventListener('keydown', (event) => {
  if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
    event.preventDefault();
    els.composer.requestSubmit();
  }
});
els.back.addEventListener('click', closeConversation);
els.simulatorForm.addEventListener('submit', simulateMessage);

state.selectedId = decodeURIComponent(location.hash.slice(1)) || null;
setLanguage(initialLanguage());
connect();
