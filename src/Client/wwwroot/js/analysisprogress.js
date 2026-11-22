// DOM updaters for the AI processing page (Pages/AiProcessing.razor).
//
// Called from Blazor when a SignalR "AnalysisEvent" arrives:
//   var module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/analysisprogress.js");
//   await module.InvokeVoidAsync("addLog", "Търсене на конкуренти...", "info");
//
// Markup hooks this file expects:
//   #terminal-output                     log lines container (its parent is the scroller)
//   [data-progress-bar]                  progress bar fill
//   [data-progress-label]                "6 / 8 МОДУЛА" text
//   [data-stage-list] > [data-stage=key] process steps
//   [data-agent-list] > [data-agent=key] AI agent cards
//
// Stages and cards that don't exist yet are created when a label/name is
// passed, so new modules can appear without touching the Razor markup.
// Text coming from the server is always written with textContent.

export const StageState = Object.freeze({
    Pending: "pending",
    Active: "active",
    Done: "done",
    Failed: "failed",
});

export const AgentState = Object.freeze({
    Pending: "pending",
    Active: "active",
    Completed: "completed",
    Failed: "failed",
});

const MAX_LOG_LINES = 200;

// ---------------------------------------------------------------- helpers

function el(tag, className, text) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text != null) node.textContent = text;
    return node;
}

function icon(name, className, filled = false) {
    const node = el("span", `material-symbols-outlined ${className ?? ""}`.trim(), name);
    node.dataset.icon = name;
    if (filled) node.style.fontVariationSettings = "'FILL' 1";
    return node;
}

function cssEscape(value) {
    return window.CSS?.escape ? CSS.escape(value) : String(value).replace(/["\\]/g, "\\$&");
}

// Works for both a normal scroller and the flex-col-reverse one in the page,
// where scrollTop is 0 at the bottom and negative when scrolled up.
function isReversed(scroller) {
    return getComputedStyle(scroller).flexDirection === "column-reverse";
}

function isAtBottom(scroller, threshold = 24) {
    if (isReversed(scroller)) return Math.abs(scroller.scrollTop) <= threshold;
    return scroller.scrollHeight - scroller.clientHeight - scroller.scrollTop <= threshold;
}

function scrollToBottom(scroller) {
    scroller.scrollTop = isReversed(scroller) ? 0 : scroller.scrollHeight;
}

// ---------------------------------------------------------------- logs

const LOG_LEVEL_CLASS = {
    info: "",
    success: "text-primary",
    warning: "text-tertiary",
    error: "text-error",
};

/**
 * Appends one line to the terminal box.
 * @param {string} text
 * @param {"info"|"success"|"warning"|"error"} [level="info"]
 */
export function addLog(text, level = "info") {
    const terminal = document.getElementById("terminal-output");
    if (!terminal) return;

    const scroller = terminal.parentElement;
    const stick = scroller ? isAtBottom(scroller) : false;

    terminal.appendChild(el("div", LOG_LEVEL_CLASS[level] ?? "", `> ${text}`));

    while (terminal.childElementCount > MAX_LOG_LINES) {
        terminal.firstElementChild.remove();
    }

    // Only follow new lines if the user hasn't scrolled up to read older ones.
    if (scroller && stick) scrollToBottom(scroller);
}

// ---------------------------------------------------------------- progress

/**
 * Sets the progress bar and the "X / Y МОДУЛА" label.
 * @param {number} completed
 * @param {number} total
 */
export function updateProgress(completed, total) {
    const safeTotal = Math.max(0, total | 0);
    const safeDone = Math.min(Math.max(0, completed | 0), safeTotal);
    const percent = safeTotal === 0 ? 0 : (safeDone / safeTotal) * 100;

    const bar = document.querySelector("[data-progress-bar]");
    if (bar) {
        // Drop the static Tailwind width (e.g. w-3/4) so the inline width wins.
        [...bar.classList].filter(c => /^w-/.test(c)).forEach(c => bar.classList.remove(c));
        bar.classList.add("transition-[width]", "duration-500", "ease-out");
        bar.style.width = `${percent}%`;
        bar.setAttribute("role", "progressbar");
        bar.setAttribute("aria-valuemin", "0");
        bar.setAttribute("aria-valuemax", String(safeTotal));
        bar.setAttribute("aria-valuenow", String(safeDone));
    }

    const label = document.querySelector("[data-progress-label]");
    if (label) label.textContent = `${safeDone} / ${safeTotal} МОДУЛА`;
}

// ---------------------------------------------------------------- stages

const STAGE_VIEW = {
    [StageState.Done]: {
        row: "flex items-center gap-sm text-on-surface-variant opacity-70",
        icon: () => icon("check_circle", "text-primary", true),
    },
    [StageState.Active]: {
        row: "flex items-center gap-sm text-primary font-bold p-xs rounded-DEFAULT bg-primary/5 border border-primary/20",
        icon: () => icon("pending", "text-primary animate-pulse-glow rounded-full"),
        badge: { text: "ОБРАБОТКА...", className: "ml-auto text-label-caps font-label-caps text-secondary-fixed-dim animate-pulse" },
    },
    [StageState.Pending]: {
        row: "flex items-center gap-sm text-surface-bright",
        icon: () => icon("radio_button_unchecked", ""),
    },
    [StageState.Failed]: {
        row: "flex items-center gap-sm text-error font-bold p-xs rounded-DEFAULT bg-error/5 border border-error/20",
        icon: () => icon("error", "text-error", true),
        badge: { text: "ГРЕШКА", className: "ml-auto text-label-caps font-label-caps text-error" },
    },
};

function readStageLabel(row) {
    const labelled = row.querySelector("[data-stage-label]");
    if (labelled) return labelled.textContent;
    // Fallback for the static markup: the label is the first non-icon span.
    const span = [...row.children].find(c => !c.classList.contains("material-symbols-outlined"));
    return span?.textContent ?? "";
}

/**
 * Sets the visual state of one process step.
 * @param {string} key    value of data-stage
 * @param {"pending"|"active"|"done"|"failed"} state
 * @param {{ label?: string, badge?: string }} [options]
 *        label: creates the stage if missing / renames it;
 *        badge: overrides the right-hand text on active/failed rows.
 */
export function updateStage(key, state, options = {}) {
    const view = STAGE_VIEW[state];
    if (!view) return;

    let row = document.querySelector(`[data-stage="${cssEscape(key)}"]`);
    if (!row) {
        const list = document.querySelector("[data-stage-list]");
        if (!list || !options.label) return;
        row = el("div");
        row.dataset.stage = key;
        list.appendChild(row);
    }

    const label = options.label ?? readStageLabel(row);

    row.className = view.row;
    row.dataset.state = state;
    const labelEl = el("span", "", label);
    labelEl.setAttribute("data-stage-label", "");
    row.replaceChildren(view.icon(), labelEl);

    if (view.badge) {
        row.appendChild(el("span", view.badge.className, options.badge ?? view.badge.text));
    }

    if (state === StageState.Active || state === StageState.Failed) {
        row.scrollIntoView({ block: "nearest", behavior: "smooth" });
    }
}

// ---------------------------------------------------------------- agent cards

const AGENT_VIEW = {
    [AgentState.Completed]: {
        card: "bg-surface-container border border-primary/10 rounded-lg p-sm flex items-center gap-sm",
        circle: "w-8 h-8 rounded-full bg-surface-variant flex items-center justify-center",
        icon: "text-sm text-primary",
        text: "",
        name: "font-label-caps text-label-caps text-primary",
        status: { text: "ПРИКЛЮЧЕН", className: "font-data-mono text-[10px] text-on-surface-variant" },
    },
    [AgentState.Active]: {
        card: "bg-surface-container border border-primary/40 rounded-lg p-sm flex items-center gap-sm agent-active relative overflow-hidden",
        overlay: "absolute inset-0 bg-primary/5",
        circle: "w-8 h-8 rounded-full bg-primary/20 flex items-center justify-center border border-primary relative z-10",
        icon: "text-sm text-primary",
        text: "relative z-10",
        name: "font-label-caps text-label-caps text-primary shadow-primary",
        status: { text: "СКАНИРАНЕ...", className: "font-data-mono text-[10px] text-secondary" },
    },
    [AgentState.Pending]: {
        card: "bg-surface-container border border-surface-bright rounded-lg p-sm flex items-center gap-sm opacity-50",
        circle: "w-8 h-8 rounded-full bg-surface-bright flex items-center justify-center",
        icon: "text-sm text-on-surface-variant",
        text: "",
        name: "font-label-caps text-label-caps text-on-surface-variant",
        status: { text: "ИЗЧАКВАНЕ", className: "font-data-mono text-[10px] text-surface-bright" },
    },
    [AgentState.Failed]: {
        card: "bg-surface-container border border-error/40 rounded-lg p-sm flex items-center gap-sm relative overflow-hidden",
        overlay: "absolute inset-0 bg-error/5",
        circle: "w-8 h-8 rounded-full bg-error/20 flex items-center justify-center border border-error relative z-10",
        icon: "text-sm text-error",
        text: "relative z-10",
        name: "font-label-caps text-label-caps text-error",
        status: { text: "ГРЕШКА", className: "font-data-mono text-[10px] text-error" },
    },
};

function readAgentInfo(card) {
    const iconName = card.querySelector(".material-symbols-outlined")?.textContent?.trim();
    const name = card.querySelector("[data-agent-name]")?.textContent
        ?? card.querySelector(".font-label-caps")?.textContent;
    return { icon: iconName, name };
}

/**
 * Sets the visual state of one AI agent card.
 * @param {string} key    value of data-agent (e.g. the module name "Market")
 * @param {"pending"|"active"|"completed"|"failed"} state
 * @param {{ name?: string, icon?: string, status?: string }} [options]
 *        name/icon: create the card if missing / change them;
 *        status: overrides the small status line (e.g. the current search query).
 */
export function updateAgentCard(key, state, options = {}) {
    const view = AGENT_VIEW[state];
    if (!view) return;

    let card = document.querySelector(`[data-agent="${cssEscape(key)}"]`);
    if (!card) {
        const list = document.querySelector("[data-agent-list]");
        if (!list || !options.name) return;
        card = el("div");
        card.dataset.agent = key;
        list.appendChild(card);
    }

    const current = readAgentInfo(card);
    const name = options.name ?? current.name ?? key;
    const iconName = options.icon ?? current.icon ?? "smart_toy";

    const circle = el("div", view.circle);
    circle.appendChild(icon(iconName, view.icon));

    const nameEl = el("div", view.name, name);
    nameEl.setAttribute("data-agent-name", "");

    const text = el("div", view.text);
    text.append(nameEl, el("div", view.status.className, options.status ?? view.status.text));

    card.className = view.card;
    card.dataset.state = state;
    card.replaceChildren(...[view.overlay && el("div", view.overlay), circle, text].filter(Boolean));
}
