export class Point {
    x: number;
    y: number;

    constructor(x: number = 0.0, y: number = 0.0) {
        this.x = x;
        this.y = y;
    }

    public add(point: Point): Point {
        return new Point(this.x + point.x, this.y + point.y);
    }
}