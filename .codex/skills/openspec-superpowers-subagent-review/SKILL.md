---
name: openspec-superpowers-subagent-review
description: Use when coordinating the required five-agent review for an OpenSpec change, covering architecture, editor/runtime boundaries, performance, tests, and data pipeline readiness.
---

# OpenSpec Superpowers Subagent Review

## Overview

用于审查 OpenSpec change。流程必须先确认范围，再启动 5 个固定审查代理，最后合并为中文审查报告。默认只读，不写实现代码。

## When To Use

- 大阶段收尾。
- 架构或数据结构变化。
- 性能敏感变更。
- 测试风险较高。
- 用户明确要求审查。

## Inputs

- OpenSpec change id。若省略，只能在当前环境恰好一个 active change 时推断；否则运行 `openspec list --json` 并请求用户指定。
- 默认使用 5 个固定 reviewer 角色；只有用户明确要求增减时才调整。
- 用户要求的审查重点。

## Read And Validate First

读取：

1. `AGENTS.md`
2. `GOALS.md`
3. `PHASES.md`
4. `.ai/workflow.md`
5. `openspec/config.yaml`
6. `openspec/changes/<change-id>/proposal.md`
7. `openspec/changes/<change-id>/design.md`
8. `openspec/changes/<change-id>/tasks.md`
9. `openspec/changes/<change-id>/specs/**/*.md`

运行：

```powershell
openspec status --change "<change-id>" --json
openspec validate --changes --strict
```

验证失败或警告必须写入最终审查。

## Dispatch Pattern

默认启动 5 个 subagent。不要把多个角色合并到同一个 agent；每个 agent 只负责一个主要视角。

固定角色映射：

- `architecture_reviewer`: 架构边界、模块归属、阶段范围、范围膨胀。
- `runtime_editor_reviewer`: Runtime / Editor 分离、编辑器专用 API 隔离、引用边界。
- `performance_reviewer`: 性能风险、GC、刷新范围、热点路径。
- `test_reviewer`: tasks 可执行性、测试覆盖、手动验收和停止条件。
- `data_pipeline_reviewer`: 数据结构、序列化、校验、兼容性、数据流。

## Standard Review Checklist

要求 reviewers 检查：

1. 是否存在超出当前阶段目标的范围膨胀。
2. Runtime 与 Editor 或其他模块边界是否清晰。
3. 数据源、数据结构、序列化和校验是否明确。
4. 是否存在性能、GC、全量刷新或重复计算风险。
5. tasks 是否太大、顺序不清或不可执行。
6. 验收标准是否清晰、可测试、可手动复现。
7. 文档、审查记录、阶段状态是否有明确同步点。

Project invariants:

- 文档和报告使用中文。
- 不猜测功能内容。
- 不从外部项目继承功能路线。
- 不把业务实现交给只读审查 agent。
- 不传递无关历史文档。

## Subagent Prompt Template

```text
请审查 OpenSpec change：<absolute-change-path>。

只读文档，不要修改文件。请阅读 proposal.md、design.md、tasks.md、specs/**/*.md，并结合 AGENTS.md、GOALS.md、PHASES.md、.ai/workflow.md、openspec/config.yaml 的约束。

请从 <role> 角度重点看：<role-specific-focus>。

输出中文审查报告：按严重度排序问题，指出具体文档位置或任务号，给出可执行修订建议。若无问题，说明残余风险。
```

## Merge Report Format

等待所有请求的 agents 返回。然后综合，不要粘贴原始报告。

使用中文结构：

1. **总体结论**
   - 是否允许进入实现。
   - OpenSpec status / validate 结果。
   - 5 个审查角色是否都已返回。

2. **阻塞问题**
   - 合并重复发现。
   - 每条包含位置、问题、影响、修订建议。

3. **中等风险**
   - 架构、数据、性能、测试、验收风险。

4. **Tasks 修订建议**
   - 哪些任务需要拆分、补充、改顺序。

5. **验收与测试建议**
   - 自动测试。
   - 手动验收。
   - OpenSpec / 编译 / 静态检查。

6. **下一步**
   - 通常是先修订 OpenSpec 文档，不写实现代码。

## Revision Mode

如果用户要求根据审查修订：

1. 只编辑 OpenSpec 文档和相关计划文档。
2. 不写业务实现代码。
3. 将合并后的发现落实为具体 requirement、design 或 task 变化。
4. 运行 `openspec validate --changes --strict`。
5. 总结修改文件和剩余风险。

## Review Record

审查完成后写入：

- `.ai/documents/reviews/<change-id>-subagent-review.md`

记录内容包括：

- 审查范围。
- 使用的 reviewer 角色。
- 合并后的 findings。
- 已回写的修订。
- 验证命令和结果。
- 残留风险。

## Verification Before Final

- 所有请求的 agents 已返回，或明确报告缺失。
- `openspec validate --changes --strict` 已运行。
- 最终报告是中文。
- Findings 已去重。
- 如修改文档，检查没有明显占位词和路径错误。

## Common Mistakes

- 审查时写实现代码。
- 审查结果良好就直接开始实现。
- 把 5 个固定 reviewers 合并成更少 agents。
- 不综合 subagent 报告。
- 未运行 OpenSpec 校验就声称通过。
- 忘记写 `.ai/documents/reviews/<change-id>-subagent-review.md`。
