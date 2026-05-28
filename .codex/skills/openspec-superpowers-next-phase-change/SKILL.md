---
name: openspec-superpowers-next-phase-change
description: Use when creating or selecting the next OpenSpec phase change under Superpowers, especially after archiving a previous phase and aligning GOALS.md and PHASES.md without writing implementation code.
---

# OpenSpec Superpowers Next Phase Change

## Overview

用于创建或选择“下一阶段”的 OpenSpec change。核心原则：先根据 `PHASES.md`、`GOALS.md` 和归档状态证明下一阶段是什么，再只写 OpenSpec 文档和必要阶段说明，不写实现代码。

## When To Use

- 用户说“创建下一个阶段的 change”。
- 用户要求按 Superpowers 流程创建 change。
- 当前阶段刚归档，需要推进到下一 Phase。
- 需要把下一阶段 proposal、design、tasks、specs 准备好，但不进入实现。

## Required Context

开始前读取：

1. `AGENTS.md`
2. `PHASES.md`
3. `GOALS.md`
4. `.ai/workflow.md`
5. `openspec/config.yaml`
6. `openspec list`
7. `openspec list --specs`
8. 上一个已归档阶段的 `proposal.md`、`design.md`、`tasks.md` 和 specs，如存在

## Workflow

1. **确认下一阶段**
   - 从 `PHASES.md` 找到当前阶段和下一阶段。
   - 确认上一阶段是否已归档。
   - 如果存在 active change，不要创建重复阶段，先报告冲突。
   - change id 必须来自 `GOALS.md` / `PHASES.md` 已声明内容；业务内容不明确时保留占位并请求用户补充。

2. **创建或选择 OpenSpec change**
   - 运行 `openspec new change "<change-id>"`。
   - 如果 change 已存在，读取现有文档并继续补齐，不覆盖用户内容。

3. **编写文档**
   - 创建或更新：
     - `proposal.md`
     - `design.md`
     - `tasks.md`
     - `specs/<capability>/spec.md`
   - 文档必须匹配当前阶段目标。
   - 不写代码，不修改实现文件。

4. **规格边界**
   - 明确 Goals / Non-Goals。
   - 明确 Runtime / Editor 或其他模块边界。
   - 每个任务必须可执行、可测试、可验收。
   - 不猜测功能内容，不从外部项目继承功能路线。

5. **同步阶段入口**
   - 需要时更新 `GOALS.md` 当前阶段、当前目标、停止条件和最小上下文。
   - 需要时更新 `PHASES.md` 当前阶段元信息。
   - 禁止同一需求同时维护多个状态源。

6. **验证**
   - 运行 `openspec validate --changes --strict`。
   - 运行 `openspec status --change "<change-id>"`。
   - 用 `rg --files openspec/changes/<change-id>` 确认文档齐全。

## Final Report

用中文报告：

- 创建或选择的 change id 和目录。
- 新增/修改的 OpenSpec 文档。
- 是否更新 `GOALS.md` / `PHASES.md`。
- OpenSpec 验证结果。
- 下一步建议：通常是 subagents 审查，不是直接实现。

## Common Mistakes

| 错误 | 正确做法 |
| --- | --- |
| 凭记忆猜下一个阶段 | 读取 `PHASES.md`、`GOALS.md` 和归档目录后再判断 |
| 已有 active change 仍创建新 change | 先报告冲突，避免并行阶段 |
| 只创建 proposal | 同时补齐 design、tasks、specs |
| 创建 change 后直接写代码 | 停在文档和验证，不进入实现 |
| 忘记同步阶段入口 | 当前阶段、只做、不做、停止条件都要对齐 |
