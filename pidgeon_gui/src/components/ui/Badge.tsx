import { HTMLAttributes, forwardRef } from 'react';
import { cn } from '@/lib/utils';

export interface BadgeProps extends HTMLAttributes<HTMLDivElement> {
  variant?: 'success' | 'danger' | 'warning' | 'info';
}

const Badge = forwardRef<HTMLDivElement, BadgeProps>(
  ({ className, variant = 'info', ...props }, ref) => {
    return (
      <div
        className={cn(
          'ph-badge',
          {
            'ph-badge-success': variant === 'success',
            'ph-badge-danger': variant === 'danger',
            'ph-badge-warning': variant === 'warning', 
            'ph-badge-info': variant === 'info',
          },
          className
        )}
        ref={ref}
        {...props}
      />
    );
  }
);

Badge.displayName = 'Badge';

export { Badge };