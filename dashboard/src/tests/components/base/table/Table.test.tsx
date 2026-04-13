import '@testing-library/jest-dom';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import LitegraphTable from '@/components/base/table/Table';
import { TableProps } from 'antd';

interface TestRecord {
  key: string;
  name: string;
}

describe('LitegraphTable', () => {
  const dataSource: TestRecord[] = [{ key: '1', name: 'Alpha' }];

  it('calls onRowClick when a non-interactive row cell is clicked', () => {
    const onRowClick = jest.fn();
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name' },
    ];

    render(
      <LitegraphTable
        columns={columns}
        dataSource={dataSource}
        pagination={false}
        onRowClick={onRowClick}
      />
    );

    fireEvent.click(screen.getByText('Alpha'));

    expect(onRowClick).toHaveBeenCalledTimes(1);
    expect(onRowClick).toHaveBeenCalledWith(dataSource[0], expect.any(Object));
  });

  it('does not call onRowClick when a button inside the row is clicked', () => {
    const onRowClick = jest.fn();
    const onButtonClick = jest.fn();
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name' },
      {
        title: 'Actions',
        key: 'actions',
        render: () => <button onClick={onButtonClick}>Action</button>,
      },
    ];

    render(
      <LitegraphTable
        columns={columns}
        dataSource={dataSource}
        pagination={false}
        onRowClick={onRowClick}
      />
    );

    fireEvent.click(screen.getByRole('button', { name: 'Action' }));

    expect(onButtonClick).toHaveBeenCalledTimes(1);
    expect(onRowClick).not.toHaveBeenCalled();
  });

  it('does not call onRowClick when a link inside the row is clicked', () => {
    const onRowClick = jest.fn();
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name' },
      {
        title: 'Details',
        key: 'details',
        render: () => (
          <a href="/details" onClick={(event) => event.preventDefault()}>
            Details
          </a>
        ),
      },
    ];

    render(
      <LitegraphTable
        columns={columns}
        dataSource={dataSource}
        pagination={false}
        onRowClick={onRowClick}
      />
    );

    fireEvent.click(screen.getByRole('link', { name: 'Details' }));

    expect(onRowClick).not.toHaveBeenCalled();
  });

  it('does not render a resize handle on the last column', () => {
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name', width: 200 },
      {
        title: 'Actions',
        key: 'actions',
        width: 80,
        render: () => <button>Action</button>,
      },
    ];

    const { container } = render(
      <LitegraphTable columns={columns} dataSource={dataSource} pagination={false} />
    );

    expect(container.querySelectorAll('.react-resizable-handle')).toHaveLength(1);
    expect(
      screen.getByRole('columnheader', { name: 'Actions' }).querySelector('.react-resizable-handle')
    ).not.toBeInTheDocument();
  });

  it('applies fixed-layout no-horizontal-scroll mode when requested', () => {
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name', width: 200 },
    ];

    const { container } = render(
      <LitegraphTable
        hideHorizontalScroll
        columns={columns}
        dataSource={dataSource}
        pagination={false}
      />
    );

    expect(container.querySelector('.litegraph-table-no-horizontal-scroll')).toBeInTheDocument();
  });
});
