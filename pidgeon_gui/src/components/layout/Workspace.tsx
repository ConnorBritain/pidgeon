'use client';

import { ReactNode, useState } from 'react';
import { cn } from '@/lib/utils';
import { ActivityBar } from './ActivityBar';
import { AIPanel } from './AIPanel';

interface WorkspaceProps {
  children: ReactNode;
  leftPanel?: ReactNode;
  renderLeftPanel?: (activeItem: string) => ReactNode;
  aiPanelOpen?: boolean;
  onAIPanelToggle?: () => void;
}

export function Workspace({ 
  children, 
  leftPanel, 
  renderLeftPanel,
  aiPanelOpen = false, 
  onAIPanelToggle 
}: WorkspaceProps) {
  // Default to Workbench and close left panel unless a leftPanel is provided
  const [activeActivity, setActiveActivity] = useState<string>('workbench');
  const [leftPanelOpen, setLeftPanelOpen] = useState(false);

  const handleActivityClick = (activityId: string) => {
    if (activityId === activeActivity && leftPanelOpen) {
      setLeftPanelOpen(false);
    } else {
      setActiveActivity(activityId);
      setLeftPanelOpen(true);
    }
  };

  const resolvedLeftPanel = renderLeftPanel ? renderLeftPanel(activeActivity) : leftPanel;

  return (
    <div className="ph-desktop-layout">
      {/* Activity Bar */}
      <ActivityBar 
        activeItem={leftPanelOpen ? activeActivity : 'workbench'}
        onItemClick={handleActivityClick}
      />
      
      {/* Main Content */}
      <div
        className="ph-main-content"
        style={{
          gridTemplateColumns: (() => {
            const left = resolvedLeftPanel && leftPanelOpen ? 'var(--ph-panel-default-width) ' : '';
            const center = '1fr';
            const right = aiPanelOpen ? ' auto' : '';
            return `${left}${center}${right}`.trim();
          })(),
        }}
      >
        {/* Left Panel */}
        {leftPanelOpen && resolvedLeftPanel && (
          <div className="ph-panel">
            {resolvedLeftPanel}
          </div>
        )}
        
        {/* Canvas */}
        <div
          className={cn(
            'ph-canvas',
            ((!leftPanelOpen || !resolvedLeftPanel) && !aiPanelOpen) && 'col-span-full'
          )}
          style={{ gridColumn: ((!leftPanelOpen || !resolvedLeftPanel) && !aiPanelOpen) ? '1 / -1' : undefined }}
        >
          {children}
        </div>
        
        {/* AI Panel */}
        {aiPanelOpen && (
          <div style={{ gridColumn: (leftPanelOpen && resolvedLeftPanel) ? 3 : 2 }}>
            <AIPanel 
              onClose={onAIPanelToggle}
            />
          </div>
        )}
      </div>
    </div>
  );
}