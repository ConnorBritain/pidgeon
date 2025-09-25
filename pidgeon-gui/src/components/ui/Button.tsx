import { ButtonHTMLAttributes, forwardRef } from 'react';
import { cn } from '@/lib/utils';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'success' | 'danger';
  size?: 'sm' | 'md' | 'lg';
}

const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = 'primary', size = 'md', ...props }, ref) => {
    return (
      <button
        className={cn(
          'ph-btn',
          {
            'ph-btn-primary': variant === 'primary',
            'ph-btn-secondary': variant === 'secondary', 
            'ph-btn-success': variant === 'success',
            'ph-btn-danger': variant === 'danger',
          },
          {
            'text-desktop-xs px-2 py-1': size === 'sm',
            'text-desktop-sm px-4 py-2': size === 'md',
            'text-desktop-lg px-6 py-3': size === 'lg',
          },
          className
        )}
        ref={ref}
        {...props}
      />
    );
  }
);

Button.displayName = 'Button';

export { Button };