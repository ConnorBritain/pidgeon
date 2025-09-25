"use client";

import { Editor } from "@monaco-editor/react";

export interface MessageEditorProps {
  value: string;
  onChange: (v: string) => void;
  height?: number | string;
}

export function MessageEditor({ value, onChange, height = "56vh" }: MessageEditorProps) {
  return (
    <div className="rounded border border-ph-gray-800 overflow-hidden">
      <Editor
        height={height}
        defaultLanguage="plaintext"
        theme="vs-dark"
        value={value}
        onChange={(v) => onChange(v ?? "")}
        options={{
          fontSize: 13,
          minimap: { enabled: false },
          scrollBeyondLastLine: false,
          wordWrap: "on",
          smoothScrolling: true,
        }}
      />
    </div>
  );
}
