# Spec: New GastroIQ design on every transactional email

**Type:** Feature   **Date:** 2026-10-31   **Requested by:** Igor Mori

## Business Goals
- One consistent, branded look in every email a person gets from GastroIQ.
- Emails read in the person's own language and market (Brazil, Canada, USA) with the same friendly tone.
- Every email says the same thing in every language, including the account-locked email.

## Objective
Give the password reset, invitation and account-locked emails the same design and tone the verification-code email already has, in four locales.

## Current State
- The verification-code email already uses the new GastroIQ design.
- Password reset and invitation use an old design under the name "Ristorante". Account locked is a plain, unbranded text page.
- Password reset and invitation exist in English, Portuguese and French. Account locked exists only in English.
- Wording differs between emails ("email" vs "e-mail", sign-offs, footers).

## Future State
| Question | Answer |
|---|---|
| **What** will change? | Password reset, invitation and account-locked emails use the verification email's design. Account locked gains Portuguese and French. The old "Ristorante" name disappears from emails. |
| **How** should it behave? | See "Shared layout", "Locales" and "Texts" below. What each email says and when it is sent do not change. |
| **Who** is impacted? | Anyone who resets a password, is invited to an organization, or has an account locked after failed sign-ins. |
| **Where** is the impact? | Modules Shared (password reset) and Identity (invitation, account locked). |

### Shared layout
Every email has, in this order: logo, white card, heading, greeting, intro, highlighted block, expiry line, grey note, sign-off, footer (help line, "sent to" line, "© year GastroIQ"). The subject and the inbox preview line are also written per email.

| Email | Highlighted block | Expiry line |
|---|---|---|
| Password reset | The reset code | Minutes the code is valid |
| Invitation | The access list (role and location, one per line) and the "Accept the invitation" button | Date the invitation expires (UTC: Coordinated Universal Time) |
| Account locked | The number of minutes sign-in stays paused | When sign-in works again, in minutes |

### Locales
| Locale | Wording | Notes |
|---|---|---|
| pt-BR | Portuguese | "e-mail"; sign-off "Equipe GastroIQ" |
| en-CA, en-US | English | Same wording for both; sign-off "The GastroIQ team" |
| fr-CA | French | "courriel"; sign-off "L’équipe GastroIQ" |

### Rules
| Rule | Detail |
|---|---|
| Fallback | Any other language gets English, as today |
| Safe values | Every name, organization, role and address shown is escaped so it cannot inject markup |
| Account locked | Still has no code, no link and no IP (Internet Protocol) address, so it never helps someone else unlock the account. The grey note tells the person to reset their password if it was not them |
| Invitation | Shows role names and locations only, never access levels or passwords |

### Texts (draft for review; `{x}` = filled in by the system)
| Part | pt-BR | English (en-CA, en-US) | fr-CA |
|---|---|---|---|
| **Reset: subject** | Seu código para redefinir a senha: {code} | Your GastroIQ password reset code: {code} | Votre code de réinitialisation GastroIQ : {code} |
| Reset: heading | Redefina sua senha | Reset your password | Réinitialisez votre mot de passe |
| Reset: intro | Digite o código abaixo no GastroIQ para redefinir sua senha. | Enter the code below in GastroIQ to reset your password. | Entrez le code ci-dessous dans GastroIQ pour réinitialiser votre mot de passe. |
| Reset: expiry | O código vale por {minutes} minutos. | The code expires in {minutes} minutes. | Le code expire dans {minutes} minutes. |
| Reset: note | Não pediu a redefinição? Pode ignorar este e-mail. Nada vai acontecer. | Didn't ask for a reset? You can ignore this email. Nothing will happen. | Vous n’avez pas demandé de réinitialisation? Vous pouvez ignorer ce courriel. Rien ne se passera. |
| Reset: sent to | Enviado para {email} porque foi pedida a redefinição de senha para este endereço. | Sent to {email} because a password reset was requested for this address. | Envoyé à {email}, car une réinitialisation du mot de passe a été demandée pour cette adresse. |
| **Invite: subject** | Você foi convidado para {organization} no GastroIQ | You are invited to join {organization} on GastroIQ | Vous êtes invité à rejoindre {organization} sur GastroIQ |
| Invite: heading | Você foi convidado | You are invited | Vous êtes invité |
| Invite: intro | {inviter} convidou você para participar de {organization} no GastroIQ. Você terá acesso como: | {inviter} invited you to join {organization} on GastroIQ. You will have access as: | {inviter} vous a invité à rejoindre {organization} sur GastroIQ. Vous aurez accès en tant que : |
| Invite: button | Aceitar o convite | Accept the invitation | Accepter l’invitation |
| Invite: expiry | Este convite expira em {date}. | This invitation expires on {date}. | Cette invitation expire le {date}. |
| Invite: note | Não esperava este convite? Pode ignorar este e-mail. Nada vai acontecer. | Not expecting this invitation? You can ignore this email. Nothing will happen. | Vous n’attendiez pas cette invitation? Vous pouvez ignorer ce courriel. Rien ne se passera. |
| Invite: sent to | Enviado para {email} porque alguém convidou este endereço. | Sent to {email} because someone invited this address. | Envoyé à {email}, car quelqu’un a invité cette adresse. |
| **Locked: subject** | O login na sua conta está pausado | Sign-in to your account is paused | La connexion à votre compte est suspendue |
| Locked: heading | Login pausado | Sign-in paused | Connexion suspendue |
| Locked: intro | Alguém tentou entrar na sua conta várias vezes com a senha errada. Por segurança, pausamos o login. | Someone tried to sign in to your account several times with a wrong password. For safety, we paused sign-in. | Quelqu’un a tenté plusieurs fois de se connecter à votre compte avec un mauvais mot de passe. Par sécurité, la connexion est suspendue. |
| Locked: block | Login pausado por {minutes} minutos | Sign-in paused for {minutes} minutes | Connexion suspendue pendant {minutes} minutes |
| Locked: expiry | Depois disso, você poderá tentar de novo. | After that, you can try again. | Après ce délai, vous pourrez réessayer. |
| Locked: note | Não foi você? Redefina sua senha. | Not you? Reset your password. | Ce n’était pas vous? Réinitialisez votre mot de passe. |
| Locked: sent to | Enviado para {email} porque houve tentativas de login nesta conta. | Sent to {email} because of sign-in attempts on this account. | Envoyé à {email}, car des tentatives de connexion ont eu lieu sur ce compte. |
| **Shared** | Olá, {first_name}, / Equipe GastroIQ / Dúvidas? É só responder este e-mail. | Hi {first_name}, / The GastroIQ team / Questions? Just reply to this email. | Bonjour {first_name}, / L’équipe GastroIQ / Des questions? Répondez simplement à ce courriel. |

## Outcomes
| # | Outcome | How we know it is achieved |
|---|---|---|
| 1 | Password reset, invitation and account-locked emails use the new layout | Each rendered email contains the logo, card, heading, greeting, intro, highlighted block, expiry line, note, sign-off and footer, and no "Ristorante" |
| 2 | Each email exists in pt-BR, en-CA, en-US and fr-CA | A rendered email for each locale shows that locale's approved texts |
| 3 | Account locked is sent in the person's language | Portuguese and French users get the Portuguese and French email |
| 4 | Unknown languages get English | A rendered email for an unsupported language shows English |
| 5 | Content is unchanged | Reset still shows its code and minutes; invitation still shows inviter, organization, roles, button link and expiry date; locked still shows the pause minutes |
| 6 | Account locked stays safe | The email has no code, no link and no IP address |
| 7 | Shown values cannot inject markup | Names with markup characters appear as plain text |

## Out of Scope
| Item |
|---|
| The verification-code email (already done); new emails, new triggers, different English for Canada and the USA |

## Backlog Impact
| Backlog file | Result |
|---|---|
| None related | Unchanged |

## Assumptions
- en-CA and en-US share one English text — the request gives no difference between them.
- Account locked uses the user's saved language like the other emails — it is the only language signal the person has.
- The invitation and locked "sent to" lines, and the locked highlighted block, are new wording — the old emails had no such parts.
