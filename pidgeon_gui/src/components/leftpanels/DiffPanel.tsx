"use client";

export function DiffPanel() {
  return (
    <div className="p-desktop-sm space-y-desktop-sm">
      <div className="ph-text-desktop-sm text-ph-gray-400">Diff (Pro)</div>
      <div className="ph-text-desktop-xs text-ph-gray-500">Compare messages or vendor profiles.</div>
      <button className="ph-btn ph-btn-secondary w-full" title="Pro feature">Open Diff</button>
    </div>
  );
}
