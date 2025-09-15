"use client";

export function DatasetsPanel() {
  return (
    <div className="p-desktop-sm space-y-desktop-sm">
      <div className="ph-text-desktop-sm text-ph-gray-400">Datasets</div>
      <div className="ph-text-desktop-xs text-ph-gray-500">Common pools for generation:</div>
      <ul className="ph-text-desktop-xs text-ph-gray-500 space-y-1">
        <li>• Names</li>
        <li>• Addresses</li>
        <li>• Medications</li>
        <li>• Facilities</li>
      </ul>
      <button className="ph-btn ph-btn-secondary w-full">Manage Datasets</button>
    </div>
  );
}
