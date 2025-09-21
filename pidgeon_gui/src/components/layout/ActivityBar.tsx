'use client';

import { cn } from '@/lib/utils';
import {
  TestTube,
  Settings,
  Folder,
  BookOpen,
  Building2,
  GitCompare,
  Database,
} from 'lucide-react';

interface ActivityBarItem {
  id: string;
  icon: React.ReactNode;
  label: string;
  shortcut?: string;
}

interface ActivityBarProps {
  activeItem?: string;
  onItemClick: (itemId: string) => void;
}

const ACTIVITY_ITEMS: ActivityBarItem[] = [
  { id: 'explorer',  icon: <Folder size={20} />,     label: 'Explorer',  shortcut: 'Cmd+Shift+E' },
  { id: 'workbench', icon: <TestTube size={20} />,    label: 'Workbench', shortcut: 'Cmd+Shift+G' },
  { id: 'library',   icon: <BookOpen size={20} />,    label: 'Library',   shortcut: 'Cmd+Shift+L' },
  { id: 'vendor',    icon: <Building2 size={20} />,   label: 'Vendor',    shortcut: 'Cmd+Shift+V' },
  { id: 'workflows', icon: <Database size={20} />,    label: 'Workflows', shortcut: 'Cmd+Shift+W' },
  { id: 'diff',      icon: <GitCompare size={20} />,  label: 'Diff',      shortcut: 'Cmd+Shift+D' },
  { id: 'datasets',  icon: <Database size={20} />,    label: 'Datasets',  shortcut: 'Cmd+Shift+S' },
];

export function ActivityBar({ activeItem, onItemClick }: ActivityBarProps) {
  return (
    <div className="ph-activity-bar">
      {/* Brand */}
      <div className="mb-desktop-sm">
        <button
          className="w-[48px] h-[48px] rounded-md overflow-hidden flex items-center justify-center bg-ph-gray-900"
          title="Pidgeon"
          aria-label="Pidgeon"
          onClick={() => onItemClick('workbench')}
        >
          <img
            src="/brand/pidgeon_logo.png"
            alt="Pidgeon"
            className="w-7 h-7 object-contain"
          />
        </button>
      </div>
      <div className="flex flex-col gap-desktop-sm">
        {ACTIVITY_ITEMS.map((item) => (
          <button
            key={item.id}
            className={cn(
              'ph-activity-item',
              {
                'ph-activity-item--active': activeItem === item.id,
              }
            )}
            onClick={() => onItemClick(item.id)}
            title={`${item.label}${item.shortcut ? ` (${item.shortcut})` : ''}`}
            aria-label={item.label}
          >
            {item.icon}
          </button>
        ))}
      </div>
      
      {/* Spacer */}
      <div className="flex-1" />
      
      {/* Bottom items */}
      <div className="flex flex-col gap-desktop-sm">
        <button
          className={cn(
            'ph-activity-item',
            {
              'ph-activity-item--active': activeItem === 'settings',
            }
          )}
          onClick={() => onItemClick('settings')}
          title="Settings (Cmd+,)"
          aria-label="Settings"
        >
          <Settings size={20} />
        </button>
      </div>
    </div>
  );
}