"use client";

export function VendorPanel() {
  return (
    <div className="p-desktop-sm space-y-desktop-sm">
      <div className="ph-text-desktop-sm text-ph-gray-400">Vendor Profiles</div>
      <button className="ph-btn ph-btn-secondary w-full">Analyze Folder…</button>
      <div className="ph-text-desktop-xs text-ph-gray-500">Active: none</div>
      <div className="ph-text-desktop-xs text-ph-gray-500">Recent Profiles
        <ul className="mt-1 space-y-1">
          <li>• epic_er.json</li>
          <li>• cerner_adt.json</li>
        </ul>
      </div>
    </div>
  );
}
