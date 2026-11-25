# CLAUDE.md

Claude Code reads this file automatically. The shared agent instructions live in `AGENTS.md` so that every coding agent follows the same rules; they are imported here:

@AGENTS.md

## Claude Code specifics

- Claude Code on the web runs in an ephemeral cloud container with a fresh clone of this repository. Commit and push work to the session's assigned branch before finishing; uncommitted work is lost when the container is reclaimed.
- Outbound network access is controlled by the environment's network policy and proxy. Never unset proxy variables or disable TLS verification; if a host or tool is blocked, report it (see `AGENTS.md` §3.2).
- Use the GitHub integration for pull requests, issues and comments, and only when asked (see `AGENTS.md` §9).
- Keep project-specific notes for Claude in this file below this line; keep rules that apply to all agents in `AGENTS.md`.
