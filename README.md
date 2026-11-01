# Xigenia

**Understand where your money goes — with plain-language explanations powered by Meta AI.**

Xigenia is a personal-finance experience for the **Meta Global AI Developer Hackathon 2026 (Indie category)**. It turns raw financial records into something you can actually read: your accounts, income, expenses, and movement history, plus an AI assistant that explains them in everyday language.

## The problem

A list of transactions rarely answers the questions people really have: *How much did I actually spend this month? Where did most of it go? What does my balance mean?* Financial records alone do not necessarily help people understand their money.

## The solution

Xigenia brings everything into one signed-in dashboard: financial accounts with opening and current balances, income and expense entries, a classified movement history, per-currency summaries — and **Financial insights**, an assistant that explains your recorded finances in plain language.

## What you can do today

Every journey below is implemented and verified in the MVP:

- **Register and sign in** with an email and password. Sessions live in memory only and clear on sign-out or refresh.
- **Create accounts** with an opening balance and currency (for example, a USD account starting at 1,000.00).
- **Record income and expenses** with dates, amounts, and notes.
- **Review balances and cash flow** — opening vs. current balance per account, plus income, expenses, and net totals per currency.
- **Inspect movement history** with income/expense classifications and transfer exclusions.
- **Ask for an explanation** through Financial insights ("Where did most of my expenses go?") and read the answer alongside a clear non-authoritative disclaimer.
- **Use it on desktop and mobile** — the dashboard adapts from wide screens down to phone widths.

## Why Meta AI matters

Xigenia keeps the model honest through a strict division of responsibility:

- **Xigenia's backend computes the facts.** Balances, totals, categories, and settlements come from deterministic, owner-scoped server calculations. The browser never computes authoritative figures.
- **Official Meta Standard (`muse-spark-1.3`) explains the facts.** The model receives only your verified records and turns them into accessible language, including suggestions clearly labeled as not from records.
- **Explanations are non-authoritative.** Every answer carries a disclaimer: only the backend records are authoritative.
- **The model changes nothing.** It cannot recalculate balances, modify transactions, or perform financial operations.

## Proof that the MVP works

In a verified live browser session, a synthetic owner with two accounts asked for an October 2026 explanation. The assistant returned the exact recorded figures — **income 500.00 USD, expenses 230.00 USD, net 270.00 USD across 6 movements** — broke spending into Groceries (200.00) and Transport (30.00), confirmed internal transfers were excluded, restated both account balances as stored, and closed with the non-authoritative notice. Backend scenarios A1–A4 were independently verified PASS, and the dashboard was accepted on desktop and mobile viewports.

## Visual showcase

All screenshots show sanitized synthetic data from verified acceptance sessions.

![Financial insights explaining grounded October 2026 figures with a category breakdown and the non-authoritative disclaimer](assets/showcase/financial-insights-success.png)

*Financial insights answering "Briefly explain my recorded October 2026 finances": income 500.00 USD, expenses 230.00 USD, net 270.00 USD, largest category Groceries — explained by Meta (`muse-spark-1.3`) with the non-authoritative disclaimer.*

![Dashboard with account creation, movement entry, USD summary, accounts table, movement history, and Financial insights](assets/showcase/dashboard-desktop.png)

*The authenticated dashboard: create accounts, record movements, review per-currency summaries and history, and ask for explanations.*

![Authenticated dashboard stacked for a phone-width viewport](assets/showcase/dashboard-mobile-authenticated.png)

*The same dashboard on a phone-width viewport — every workflow remains usable.*

![First-run empty states guiding a new owner](assets/showcase/empty-owner-mobile.png)

*First run: empty states guide a new owner to create their first account.*

![Sign-in with in-memory-only sessions](assets/showcase/login-desktop.png)

*Sign-in uses the existing Identity API; tokens stay in memory and never touch browser storage.*

## Privacy and responsible design

Only safeguards present in the shipped MVP are claimed:

- Sign-in tokens stay in memory; nothing is persisted in browser storage, and private views clear on logout, refresh, or session replacement.
- Each owner sees only their own records; ownership is derived server-side and never trusted from the client.
- AI explanations are labeled non-authoritative and cannot write or modify financial data.
- No audits, compliance certifications, encryption properties, or security guarantees are claimed beyond what was observed in verification.

## Current scope and future direction

**Shipped in this MVP:** registration and sign-in, accounts, income/expense entry, movement history, per-currency summaries, responsive dashboard, and grounded Meta AI explanations.

**Not yet demonstrated:** budgeting, savings goals, OCR, public hosting, and any feature beyond the journeys above. These are ideas, not claims.

## Hackathon and evaluation

Built for the **Meta Global AI Developer Hackathon 2026 — Indie category**. No public demo or hosted deployment is claimed; hosting remains pending.

*Evaluator access (short version):* the MVP runs locally with the .NET 10 SDK and a SQL Server database; AI explanations additionally need a Meta API key, and the app remains fully usable without one.
