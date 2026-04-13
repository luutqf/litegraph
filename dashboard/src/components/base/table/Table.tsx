import React, { useEffect, useState } from 'react';
import { Table, TableProps } from 'antd';
import { Resizable } from 'react-resizable';
import LitegraphText from '../typograpghy/Text';

const ResizableTitle = (props: any) => {
  const { onResize, width, ...restProps } = props;

  if (!width) {
    return <th {...restProps} />;
  }

  return (
    <Resizable
      width={width}
      height={0}
      handle={
        <span
          className="react-resizable-handle"
          onClick={(e) => {
            e.stopPropagation();
          }}
        />
      }
      onResize={onResize}
      draggableOpts={{ enableUserSelectHack: false }}
    >
      <th {...restProps} />
    </Resizable>
  );
};

interface LitegraphTableProps<T = any> extends TableProps<T> {
  showTotal?: boolean;
  hideHorizontalScroll?: boolean;
  onRowClick?: (record: T, event: React.MouseEvent<HTMLElement>) => void;
}

const interactiveRowClickSelector = [
  'a[href]',
  'button',
  'input',
  'textarea',
  'select',
  'label',
  '[role="button"]',
  '[role="link"]',
  '[role="menuitem"]',
  '[data-row-click-ignore="true"]',
  '.ant-btn',
  '.ant-checkbox-wrapper',
  '.ant-dropdown',
  '.ant-dropdown-trigger',
  '.ant-pagination',
  '.ant-select',
  '.ant-switch',
].join(',');

const shouldIgnoreRowClick = (target: EventTarget | null) => {
  return target instanceof HTMLElement && Boolean(target.closest(interactiveRowClickSelector));
};

const LitegraphTable = <T extends object = any>(props: LitegraphTableProps<T>) => {
  const {
    columns,
    dataSource,
    showTotal = true,
    pagination,
    onRow,
    onRowClick,
    hideHorizontalScroll,
    className,
    tableLayout,
    ...rest
  } = props;
  const [columnsState, setColumnsState] = useState(columns);

  const handleResize =
    (index: number) =>
    (e: any, { size }: any) => {
      setColumnsState((prev: any) => {
        const nextColumns = [...prev];
        nextColumns[index] = {
          ...nextColumns[index],
          width: size.width,
        };
        return nextColumns;
      });
    };

  const columnsWithResizable = columnsState?.map((col: any, index: number) => {
    const isLastColumn = index === columnsState.length - 1;

    if (isLastColumn) {
      return col;
    }

    return {
      ...col,
      onHeaderCell: (column: any) => ({
        width: column.width,
        onResize: handleResize(index),
      }),
    };
  });

  useEffect(() => {
    setColumnsState(columns);
  }, [columns]);

  const paginationWithTotal =
    pagination !== false
      ? {
          ...((typeof pagination === 'object' ? pagination : {}) as object),
          showTotal: showTotal
            ? (total: number) => (
                <LitegraphText style={{ marginRight: 8 }}>
                  Total: <strong>{total}</strong> records
                </LitegraphText>
              )
            : undefined,
        }
      : false;

  const getRowProps: TableProps<T>['onRow'] = (record, index) => {
    const rowProps = onRow?.(record, index) || {};

    if (!onRowClick) {
      return rowProps;
    }

    const { onClick, style, className, ...restRowProps } = rowProps;

    return {
      ...restRowProps,
      className: [className, 'litegraph-clickable-row'].filter(Boolean).join(' ') || undefined,
      style: { cursor: 'pointer', ...style },
      onClick: (event) => {
        onClick?.(event);

        if (event.defaultPrevented || shouldIgnoreRowClick(event.target)) {
          return;
        }

        onRowClick(record, event);
      },
    };
  };

  return (
    <Table
      {...rest}
      className={[className, hideHorizontalScroll ? 'litegraph-table-no-horizontal-scroll' : '']
        .filter(Boolean)
        .join(' ')}
      dataSource={dataSource}
      columns={columnsWithResizable}
      pagination={paginationWithTotal}
      onRow={getRowProps}
      tableLayout={hideHorizontalScroll ? 'fixed' : tableLayout}
      components={{
        header: {
          cell: ResizableTitle,
        },
      }}
    />
  );
};

export default LitegraphTable;
