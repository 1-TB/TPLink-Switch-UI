import { Card, CardContent } from './ui/card';

export default function SystemInfoCard({ info }: { info: any }) {
  return (
    <Card className="mt-4">
      <CardContent className="pt-6">
        <h2 className="text-xl font-semibold mb-4">System Information</h2>
        <ul className="space-y-2">
          {Object.entries(info).map(([k, v]) => (
            <li key={k} className="flex justify-between">
              <span className="font-medium text-muted-foreground capitalize">
                {k.replace(/([A-Z])/g, ' $1')}:
              </span>
              <span>{String(v)}</span>
            </li>
          ))}
        </ul>
      </CardContent>
    </Card>
  );
}