'use client';

import { Menu, MenuProps } from 'antd';
import React, { useMemo } from 'react';
import { MenuItemProps } from './types';
import { useAppDynamicNavigation } from '@/hooks/hooks';
import { ItemType } from 'antd/es/menu/interface';
import Link from 'next/link';
import { usePathname } from 'next/navigation';

interface MenuItemsProps extends MenuProps {
  menuItems: MenuItemProps[];
  handleClickMenuItem?: (item: MenuItemProps) => void;
}

const MenuItems = ({ menuItems, handleClickMenuItem, ...rest }: MenuItemsProps) => {
  const { serializePath } = useAppDynamicNavigation();
  const pathname = usePathname();

  const selectedKeys = useMemo(() => {
    const find = (items: MenuItemProps[]): string[] => {
      for (const item of items) {
        if (item.path) {
          const serialized = serializePath(item.path);
          if (serialized && pathname === serialized) {
            return [item.key];
          }
        }
        if (item.children) {
          const childMatch = find(item.children);
          if (childMatch.length > 0) return childMatch;
        }
      }
      return [];
    };
    return find(menuItems);
  }, [menuItems, pathname, serializePath]);

  const convertToMenuItems = (items: MenuItemProps[]): ItemType[] =>
    items.map((item: MenuItemProps) => {
      if (item.children) {
        return {
          key: item.key,
          icon: item.icon,
          label: item.label,
          children: convertToMenuItems(item.children),
        };
      }
      const href = serializePath(item.path) || '#';
      return {
        key: item.key,
        icon: item.icon,
        label: (
          <Link
            href={href}
            style={{ color: 'inherit', textDecoration: 'none', display: 'block' }}
            onClick={() => handleClickMenuItem && handleClickMenuItem(item)}
          >
            {item.label}
          </Link>
        ),
        title: item.title || item.label,
      };
    });

  return (
    <Menu
      {...rest}
      mode="inline"
      selectedKeys={selectedKeys}
      items={convertToMenuItems(menuItems)}
    />
  );
};

export default MenuItems;
